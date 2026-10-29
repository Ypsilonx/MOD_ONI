using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace DllStripper
{
    /// <summary>
    /// Odstraní z DLL typy, jejichž načtení vyžaduje assembly, které ve hře nejsou (např. System.Xaml).
    /// Hra volá GetTypes() na všech assembly v procesu a jediný takový typ ji shodí.
    /// Typ se odstraní, když na zakázanou assembly (nebo na už odstraněný typ) odkazuje jeho základní typ,
    /// rozhraní, pole, vlastnost, signatura metody nebo atribut; opakuje se, dokud se něco odstraňuje.
    /// Metody ponechaných typů, jejichž tělo na odstraněné nebo zakázané typy sahá, dostanou místo těla
    /// pahýl „throw NotSupportedException“ – jde o cesty (Remoting, XAML), které ve hře stejně nefungují.
    /// </summary>
    internal static class Program
    {
        /// <summary>Použití: DllStripper &lt;dll&gt; &lt;zakázaná assembly&gt;... – upraví DLL na místě.</summary>
        /// <returns>0 = OK, 1 = chybné argumenty.</returns>
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Použití: DllStripper <dll> <zakázaná assembly>...");
                return 1;
            }
            string path = args[0];
            var forbidden = new HashSet<string>(args.Skip(1), StringComparer.OrdinalIgnoreCase);

            // Načíst do paměti, ať jde zapsat zpět do stejného souboru.
            var module = ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(path)));
            var removed = new HashSet<string>();

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var type in AllTypes(module).ToList())
                {
                    if (removed.Contains(type.FullName) || !IsBroken(type, forbidden, removed))
                        continue;
                    if (type.IsNested)
                        type.DeclaringType.NestedTypes.Remove(type);
                    else
                        module.Types.Remove(type);
                    // Vnořené typy odcházejí s rodičem.
                    foreach (var t in AllNested(type))
                        removed.Add(t.FullName);
                    changed = true;
                }
            }

            if (removed.Count == 0)
            {
                Console.WriteLine($"DllStripper: {Path.GetFileName(path)} – nic k odstranění.");
                return 0;
            }
            var stubbed = StubBrokenBodies(module, forbidden, removed);

            // Přes dočasný soubor: nepovedený zápis nesmí poškodit původní DLL.
            string tmp = path + ".tmp";
            module.Write(tmp);
            File.Move(tmp, path, overwrite: true);

            Console.WriteLine($"DllStripper: {Path.GetFileName(path)} – odstraněno {removed.Count} typů, " +
                              $"{stubbed.Count} metod nahrazeno pahýlem:");
            foreach (var name in removed.OrderBy(n => n))
                Console.WriteLine("  typ:    " + name);
            foreach (var name in stubbed.OrderBy(n => n))
                Console.WriteLine("  pahýl:  " + name);
            return 0;
        }

        /// <summary>
        /// Nahradí tělo každé metody, která v IL odkazuje na odstraněný/zakázaný typ nebo jeho člen,
        /// za „throw new NotSupportedException(...)“.
        /// </summary>
        /// <returns>Plná jména nahrazených metod.</returns>
        private static List<string> StubBrokenBodies(ModuleDefinition module, HashSet<string> forbidden, HashSet<string> removed)
        {
            bool Bad(TypeReference r) => Uses(r, forbidden, removed);
            bool BadMember(MemberReference m) => m switch
            {
                MethodReference mr => Bad(mr.DeclaringType) || Bad(mr.ReturnType) || mr.Parameters.Any(p => Bad(p.ParameterType)),
                FieldReference fr => Bad(fr.DeclaringType) || Bad(fr.FieldType),
                _ => false,
            };

            // NotSupportedException z mscorlib, na kterou DLL už odkazuje (ne z běhového prostředí nástroje).
            var mscorlib = module.AssemblyReferences.First(a => a.Name == "mscorlib");
            var exType = new TypeReference("System", "NotSupportedException", module, mscorlib);
            var exCtor = new MethodReference(".ctor", module.TypeSystem.Void, exType) { HasThis = true };
            exCtor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));

            var stubbed = new List<string>();
            foreach (var method in AllTypes(module).SelectMany(t => t.Methods).Where(m => m.HasBody))
            {
                var body = method.Body;
                bool broken = body.Variables.Any(v => Bad(v.VariableType))
                    || body.ExceptionHandlers.Any(h => Bad(h.CatchType))
                    || body.Instructions.Any(i => i.Operand switch
                    {
                        TypeReference tr => Bad(tr),
                        MemberReference mr => BadMember(mr),
                        _ => false,
                    });
                if (!broken)
                    continue;

                body.Instructions.Clear();
                body.Variables.Clear();
                body.ExceptionHandlers.Clear();
                var il = body.GetILProcessor();
                il.Emit(Mono.Cecil.Cil.OpCodes.Ldstr, "Removed by DllStripper (not available in Oxygen Not Included)");
                il.Emit(Mono.Cecil.Cil.OpCodes.Newobj, exCtor);
                il.Emit(Mono.Cecil.Cil.OpCodes.Throw);
                stubbed.Add(method.FullName);
            }
            return stubbed;
        }

        /// <summary>Všechny typy modulu včetně vnořených.</summary>
        private static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module) =>
            module.Types.SelectMany(AllNested);

        /// <summary>Typ a rekurzivně všechny jeho vnořené typy.</summary>
        private static IEnumerable<TypeDefinition> AllNested(TypeDefinition type) =>
            new[] { type }.Concat(type.NestedTypes.SelectMany(AllNested));

        /// <summary>Jestli načtení typu vyžaduje zakázanou assembly nebo odstraněný typ.</summary>
        private static bool IsBroken(TypeDefinition type, HashSet<string> forbidden, HashSet<string> removed)
        {
            bool Bad(TypeReference r) => Uses(r, forbidden, removed);

            if (type.BaseType != null && Bad(type.BaseType)) return true;
            if (type.Interfaces.Any(i => Bad(i.InterfaceType))) return true;
            if (type.CustomAttributes.Any(a => Bad(a.AttributeType))) return true;
            if (type.GenericParameters.Any(g => g.Constraints.Any(c => Bad(c.ConstraintType)))) return true;
            if (type.Fields.Any(f => Bad(f.FieldType))) return true;
            if (type.Properties.Any(p => Bad(p.PropertyType))) return true;
            if (type.Events.Any(e => Bad(e.EventType))) return true;
            return type.Methods.Any(m => Bad(m.ReturnType) || m.Parameters.Any(p => Bad(p.ParameterType)));
        }

        /// <summary>Jestli odkaz na typ (i generický, pole, ukazatel) vede do zakázané assembly nebo na odstraněný typ.</summary>
        private static bool Uses(TypeReference r, HashSet<string> forbidden, HashSet<string> removed)
        {
            switch (r)
            {
                case null:
                    return false;
                case GenericInstanceType g:
                    return Uses(g.ElementType, forbidden, removed) || g.GenericArguments.Any(a => Uses(a, forbidden, removed));
                case TypeSpecification s:
                    return Uses(s.ElementType, forbidden, removed);
                case GenericParameter _:
                    return false;
            }
            if (removed.Contains(r.FullName)) return true;
            return r.Scope is AssemblyNameReference a && forbidden.Contains(a.Name);
        }
    }
}
