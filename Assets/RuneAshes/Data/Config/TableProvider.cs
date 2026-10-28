using System.Collections.Generic;

namespace RuneAshes.Data
{
    public sealed class TableProvider : ITableProvider
    {
        public RuneTable Runes { get; private set; }

        public void LoadAll()
        {
            Load(RuneTableData.All);
        }

        public void Load(IEnumerable<RuneDef> runes)
        {
            Runes = new RuneTable(runes);
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (Runes == null)
            {
                errors.Add("Rune table is not loaded. Call LoadAll() first.");
                return errors;
            }

            if (Runes.Count == 0)
            {
                errors.Add("Rune table is empty.");
                return errors;
            }

            foreach (var rune in Runes.All)
            {
                if (string.IsNullOrWhiteSpace(rune.Name))
                {
                    errors.Add(rune.Id + ": name is empty.");
                }

                if (rune.EnergyCost < 0)
                {
                    errors.Add(rune.Id + ": energy_cost must be >= 0.");
                }

                if (rune.MaxLevel < 0)
                {
                    errors.Add(rune.Id + ": max_level must be >= 0.");
                }

                if (float.IsNaN(rune.BaseValue) || float.IsInfinity(rune.BaseValue))
                {
                    errors.Add(rune.Id + ": base_value must be finite.");
                }

                if (rune.Category == RuneCategory.Effect && rune.EnergyCost == 0)
                {
                    errors.Add(rune.Id + ": effect runes must cost energy (0 would make them free picks).");
                }

                if (rune.Category == RuneCategory.Modifier && rune.EnergyCost == 0)
                {
                    errors.Add(rune.Id + ": modifier runes must cost energy.");
                }
            }

            return errors;
        }
    }
}
