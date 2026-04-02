using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WealthBeyondMeasure
{
    public class WealthBeyondMeasureMod : Mod
    {
        public static WealthSettings Settings;

        private Vector2 settingsScrollPosition = Vector2.zero;
        private readonly Dictionary<string, string> floatBuffers = new Dictionary<string, string>();

        public WealthBeyondMeasureMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WealthSettings>();
            Settings.EnsureDefaultsLoaded();
        }

        public override string SettingsCategory()
        {
            return "Wealth Beyond Measure";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.EnsureDefaultsLoaded();

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 1700f + (Settings.workTypePayRates.Count + Settings.consumablePrices.Count) * 30f);
            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, viewRect);

            float y = 0f;
            DrawSectionHeader(ref y, viewRect.width, "WBM_Settings_GeneralHeader".Translate());
            DrawCurrencySelector(ref y, viewRect.width);
            DrawPaydaySelector(ref y, viewRect.width);
            DrawCheckbox(ref y, viewRect.width, "WBM_Settings_ShowOverlay".Translate(), ref Settings.showOverlay);
            DrawCheckbox(ref y, viewRect.width, "WBM_Settings_InfiniteBank".Translate(), ref Settings.infiniteBankSilver);
            y += 12f;

            DrawSectionHeader(ref y, viewRect.width, "WBM_Settings_WorkRatesHeader".Translate());
            Widgets.Label(new Rect(0f, y, viewRect.width, 24f), "WBM_Settings_WorkRatesDescription".Translate());
            y += 28f;
            foreach (WorkTypePayRateEntry entry in Settings.workTypePayRates.ToList())
            {
                WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.workTypeDefName);
                if (def == null)
                {
                    continue;
                }

                DrawDecimalEntry(ref y, viewRect.width, def.labelShort.CapitalizeFirst(), "work_" + entry.workTypeDefName, entry.silverPerHour, value => entry.silverPerHour = Mathf.Max(0f, value));
            }

            y += 12f;
            DrawSectionHeader(ref y, viewRect.width, "WBM_Settings_ItemCostsHeader".Translate());
            Widgets.Label(new Rect(0f, y, viewRect.width, 36f), "WBM_Settings_ItemCostsDescription".Translate());
            y += 40f;
            foreach (ConsumablePriceEntry entry in Settings.consumablePrices.ToList())
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(entry.thingDefName);
                if (def == null)
                {
                    continue;
                }

                string label = def.label.CapitalizeFirst();
                if (def.IsMedicine)
                {
                    label += " (Medicine)";
                }
                else
                {
                    label += " (Food)";
                }

                DrawDecimalEntry(ref y, viewRect.width, label, "item_" + entry.thingDefName, entry.silverCost, value => entry.silverCost = Mathf.Max(0f, value));
            }

            Widgets.EndScrollView();
            Settings.Write();
        }

        private void DrawCurrencySelector(ref float y, float width)
        {
            Widgets.Label(new Rect(0f, y, width * 0.6f, 24f), "WBM_Settings_CurrencyName".Translate());
            Rect buttonRect = new Rect(width * 0.62f, y, 180f, 24f);
            if (Widgets.ButtonText(buttonRect, CurrencyUtility.GetOptionLabel(Settings.currencyStyleIndex)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                for (int i = 0; i < CurrencyUtility.OptionCount; i++)
                {
                    int capture = i;
                    options.Add(new FloatMenuOption(CurrencyUtility.GetOptionLabel(capture), delegate
                    {
                        Settings.currencyStyleIndex = capture;
                                }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect descRect = new Rect(0f, y + 26f, width, 36f);
            Widgets.Label(descRect, "WBM_Settings_CurrencyDescription".Translate(CurrencyUtility.GetCurrentLabelCapitalized()));
            y += 66f;
        }

        private void DrawPaydaySelector(ref float y, float width)
        {
            List<string> days = PaydayUtility.WeekdayLabels;
            Widgets.Label(new Rect(0f, y, width * 0.6f, 24f), "WBM_Settings_PaydayDay".Translate());
            Rect buttonRect = new Rect(width * 0.62f, y, 180f, 24f);
            if (Widgets.ButtonText(buttonRect, days[Mathf.Clamp(Settings.paydayDayIndex, 0, days.Count - 1)]))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                for (int i = 0; i < days.Count; i++)
                {
                    int capture = i;
                    options.Add(new FloatMenuOption(days[i], delegate
                    {
                        Settings.paydayDayIndex = capture;
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            y += 32f;
        }

        private void DrawSectionHeader(ref float y, float width, string label)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 32f), label);
            Text.Font = GameFont.Small;
            y += 34f;
        }

        private void DrawCheckbox(ref float y, float width, string label, ref bool value)
        {
            Widgets.CheckboxLabeled(new Rect(0f, y, width, 24f), label, ref value);
            y += 28f;
        }

        private void DrawDecimalEntry(ref float y, float width, string label, string key, float value, System.Action<float> setter)
        {
            Widgets.Label(new Rect(0f, y, width * 0.62f, 24f), label);
            Rect fieldRect = new Rect(width * 0.66f, y, 120f, 24f);

            if (!floatBuffers.TryGetValue(key, out string buffer))
            {
                buffer = value.ToString("0.0#", CultureInfo.InvariantCulture);
            }

            Widgets.TextFieldNumeric(fieldRect, ref value, ref buffer, 0f, 10000f);
            floatBuffers[key] = buffer;
            setter(value);
            y += 28f;
        }
    }
}
