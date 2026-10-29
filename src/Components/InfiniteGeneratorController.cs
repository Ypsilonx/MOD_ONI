using InfiniteGenerator;
using UnityEngine;

// Záměrně v globálním namespace: hra ukládá komponenty do savu pod plným jménem typu
// (GetType().ToString()), přejmenování nebo přidání namespace by rozbilo načítání starých savů.

/// <summary>
/// Řídí výrobu energie generátoru: každých 200 ms doplní Generatoru joule podle nastaveného
/// výkonu, ovládá animaci a poskytuje posuvník výkonu v bočním panelu.
/// </summary>
public class InfiniteGeneratorController : KMonoBehaviour, ISim200ms, ISingleSliderControl
{
#pragma warning disable CS0649 // Pole plní hra přes [MyCmpGet] za běhu.
    [MyCmpGet]
    private KBatchedAnimController animController;

    [MyCmpGet]
    private Generator generator;

    [MyCmpGet]
    private Operational operational;
#pragma warning restore CS0649

    private float wattageRating = 5000f;
    private const float MinWattage = 100f;
    private const float MaxWattage = 10000f;

    /// <summary>Po vzniku budovy nastaví výkon Generatoru na aktuální hodnotu.</summary>
    protected override void OnSpawn()
    {
        base.OnSpawn();

        if (generator != null)
        {
            var wattageField = typeof(Generator).GetField("wattageRating",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wattageField != null)
            {
                wattageField.SetValue(generator, wattageRating);
            }
        }

        Debug.Log($"{InfiniteGeneratorMod.LogPrefix} InfiniteGenerator spawned with {wattageRating}W");
    }

    /// <summary>
    /// Periodická logika: když je budova v provozu, doplní joule za uplynulý čas a pustí animaci;
    /// jinak generátor deaktivuje a animaci zastaví.
    /// </summary>
    /// <param name="dt">Uplynulý čas v sekundách od posledního volání.</param>
    public void Sim200ms(float dt)
    {
        if (generator == null || operational == null)
            return;

        if (operational.IsOperational)
        {
            var joulesField = typeof(Generator).GetField("joulesAvailable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (joulesField != null)
            {
                float maxJoules = wattageRating * dt; // W * s = J
                joulesField.SetValue(generator, maxJoules);
            }
            operational.SetActive(true);

            // Animace běží jen při výrobě, rychlost podle výkonu.
            if (animController != null)
            {
                float speed = Mathf.Lerp(0.2f, 2.0f, (wattageRating - MinWattage) / (MaxWattage - MinWattage));
                var animSpeedField = typeof(KBatchedAnimController).GetField("animSpeed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animSpeedField != null)
                    animSpeedField.SetValue(animController, speed);
                if (animController.currentAnim.ToString() != "on")
                    animController.Play("on", KAnim.PlayMode.Loop);
            }
        }
        else
        {
            operational.SetActive(false);
            if (animController != null)
            {
                var animSpeedField = typeof(KBatchedAnimController).GetField("animSpeed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animSpeedField != null)
                    animSpeedField.SetValue(animController, 1f);
                if (animController.currentAnim.ToString() != "off")
                    animController.Play("off", KAnim.PlayMode.Loop);
            }
        }

        // Kvůli zobrazení výkonu v infoboxu (pozor: BuildingDef je sdílený všemi instancemi).
        if (generator != null)
        {
            var def = generator.GetComponent<Building>().Def;
            if (def != null)
            {
                def.GeneratorWattageRating = wattageRating;
            }
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

    /// <summary>Nastaví nový výkon a hned ho propíše do Generatoru.</summary>
    /// <param name="value">Nový výkon ve W.</param>
    /// <param name="index">Index posuvníku (máme jen jeden).</param>
    public void SetSliderValue(float value, int index)
    {
        wattageRating = value;
        if (generator != null)
        {
            var wattageField = typeof(Generator).GetField("wattageRating",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wattageField != null)
            {
                wattageField.SetValue(generator, wattageRating);
            }

            var def = generator.GetComponent<Building>().Def;
            if (def != null)
            {
                def.GeneratorWattageRating = wattageRating;
            }
        }
    }

    /// <summary>Klíč tooltipu posuvníku.</summary>
    public string GetSliderTooltipKey(int index) => ModStrings.SliderTooltipKey;

    /// <summary>Text tooltipu posuvníku.</summary>
    public string GetSliderTooltip(int index) => ModStrings.SliderTooltip;
}
