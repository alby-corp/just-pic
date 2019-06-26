namespace JE2Sql
{
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization.Formatters.Binary;

    public class PersistedCookieContainer
    {
        readonly string path;
        readonly BinaryFormatter formatter = new BinaryFormatter();

        public PersistedCookieContainer(string path)
        {
            this.path = path;

            Container = Read();
        }

        CookieContainer Read()
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open);

                return (CookieContainer) formatter.Deserialize(stream);
            }
            catch (FileNotFoundException)
            {
                return new CookieContainer();
            }
        }

        public void Save()
        {
            using var stream = File.Create(path);

            formatter.Serialize(stream, Container);
        }

        public CookieContainer Container { get; }
    }
}