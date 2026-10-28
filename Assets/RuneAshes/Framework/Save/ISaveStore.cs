namespace RuneAshes.Framework
{
    public interface ISaveStore
    {
        string SavePath { get; }

        bool HasSave { get; }

        SaveFile Load();

        void Save(SaveFile data);

        void Delete();
    }
}
