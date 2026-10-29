using InfiniteGenerator;
using KSerialization;
using UnityEngine;

// Záměrně v globálním namespace: hra ukládá komponenty do savu pod plným jménem typu
// (GetType().ToString()), přejmenování nebo přidání namespace by rozbilo načítání starých savů.

/// <summary>
/// Řídí výrobu energie generátoru: každých 200 ms vyrobí joule podle nastaveného výkonu,
/// ovládá animaci a poskytuje posuvník výkonu v bočním panelu. Výkon je pro každou budovu
/// zvlášť a ukládá se do savu.
/// </summary>
[SerializationConfig(MemberSerialization.OptIn)]
public class InfiniteGeneratorController : KMonoBehaviour, ISim200ms, ISingleSliderControl
{
    /// <summary>Výchozí výkon nově postavené budovy ve W.</summary>
    public const float DefaultWattage = 5000f;
    private const float MinWattage = 100f;
    private const float MaxWattage = 10000f;

#pragma warning disable CS0649 // Pole plní hra přes [MyCmpGet] za běhu.
    [MyCmpGet]
    private KBatchedAnimController animController;

    [MyCmpGet]
    private Generator generator;

    [MyCmpGet]
    private Operational operational;
#pragma warning restore CS0649

    [Serialize]
    private float wattageRating = DefaultWattage;

    /// <summary>Jestli generátor v minulém ticku vyráběl; animace se přepíná jen při změně.</summary>
    private bool wasActive;

    /// <summary>Aktuální výkon této budovy ve W (čte ho patch na Generator.WattageRating).</summary>
    public float WattageRating => wattageRating;

    /// <summary>Po vzniku budovy (i po načtení savu) nastaví animaci podle stavu.</summary>
    protected override void OnSpawn()
    {
        base.OnSpawn();
        wasActive = !operational.IsOperational; // vynutí první přepnutí animace v Sim200ms
        Debug.Log($"{InfiniteGeneratorMod.LogPrefix} InfiniteGenerator spawned with {wattageRating}W");
    }

    /// <summary>
    /// Periodická logika: když je budova v provozu, vyrobí joule za uplynulý čas;
    /// jinak generátor deaktivuje.
    /// </summary>
    /// <param name="dt">Uplynulý čas v sekundách od posledního volání.</param>
    public void Sim200ms(float dt)
    {
        bool active = operational.IsOperational;
        if (active)
        {
            // GenerateJoules se zapíše i do denního reportu energie; přebytek nad kapacitou
            // (GeneratorBaseCapacity) hra zahodí a vykáže jako ztrátu, stejně jako u ostatních generátorů.
            generator.GenerateJoules(wattageRating * dt);
        }
        operational.SetActive(active);

        if (active != wasActive)
        {
            wasActive = active;
            UpdateAnimation();
        }
    }

    /// <summary>Pustí animaci "on" s rychlostí podle výkonu, nebo "off" při vypnutí.</summary>
    private void UpdateAnimation()
    {
        if (wasActive)
        {
            animController.PlaySpeedMultiplier =
                Mathf.Lerp(0.2f, 2.0f, (wattageRating - MinWattage) / (MaxWattage - MinWattage));
            animController.Play("on", KAnim.PlayMode.Loop);
        }
        else
        {
            animController.PlaySpeedMultiplier = 1f;
            animController.Play("off", KAnim.PlayMode.Loop);
        }
    }

    // --- ISingleSliderControl: posuvník výkonu v bočním panelu ---

    /// <summary>Klíč nadpisu posuvníku.</summary>
    public string SliderTitleKey => ModStrings.SliderTitleKey;

    /// <summary>Jednotka zobrazená u posuvníku.</summary>
    public string SliderUnits => "W";

    /// <summary>Počet desetinných míst hodnoty posuvníku.</summary>
    public int SliderDecimalPlaces(int index) => 0;

    /// <summary>Minimální výkon.</summary>
    public float GetSliderMin(int index) => MinWattage;

    /// <summary>Maximální výkon.</summary>
    public float GetSliderMax(int index) => MaxWattage;

    /// <summary>Aktuální výkon.</summary>
    public float GetSliderValue(int index) => wattageRating;

    /// <summary>Nastaví nový výkon této budovy a hned upraví rychlost animace.</summary>
    /// <param name="value">Nový výkon ve W.</param>
    /// <param name="index">Index posuvníku (máme jen jeden).</param>
    public void SetSliderValue(float value, int index)
    {
        wattageRating = Mathf.Clamp(value, MinWattage, MaxWattage);
        if (wasActive)
            UpdateAnimation();
    }

    /// <summary>Klíč tooltipu posuvníku.</summary>
    public string GetSliderTooltipKey(int index) => ModStrings.SliderTooltipKey;

    /// <summary>Text tooltipu posuvníku.</summary>
    public string GetSliderTooltip(int index) => ModStrings.SliderTooltip;
}
