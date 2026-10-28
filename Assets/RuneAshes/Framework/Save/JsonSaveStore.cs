using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace RuneAshes.Framework
{
    /// <summary>
    /// JSON 存档：可读、可迁移、可回滚。
    /// 原子写（tmp -> Replace）+ 备份档 + SHA256 校验，防断电与手改损坏。
    /// 主档与备份档各自拥校验文件，否则回退时会被主档的 hash 误判。
    /// </summary>
    public sealed class JsonSaveStore : ISaveStore
    {
        private readonly string _path;
        private readonly string _backupPath;
        private readonly string _hashPath;
        private readonly string _backupHashPath;

        public JsonSaveStore(string directory = null, string fileName = "save.json")
        {
            var root = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            _path = Path.Combine(root, fileName);
            _backupPath = _path + ".bak";
            _hashPath = _path + ".sha256";
            _backupHashPath = _backupPath + ".sha256";
        }

        public string SavePath
        {
            get { return _path; }
        }

        public bool HasSave
        {
            get { return File.Exists(_path) || File.Exists(_backupPath); }
        }

        public SaveFile Load()
        {
            var data = TryLoad(_path, _hashPath);
            if (data != null)
            {
                return SaveMigrations.Migrate(data);
            }

            data = TryLoad(_backupPath, _backupHashPath);
            if (data != null)
            {
                Debug.LogWarning("[Save] Primary save unreadable, loaded backup.");
                return SaveMigrations.Migrate(data);
            }

            return new SaveFile();
        }

        /// <summary>
        /// 原子写：先写 .tmp，再替换正式档，旧主档自动变成 .bak。
        /// 任何一步断电，最多丢掉"这一次写入"，不会留下半个 JSON。
        /// 主档与备份档各自维护 .sha256，所以回退时不会拿主档的 hash 去校验备份档。
        /// </summary>
        public void Save(SaveFile data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.Version = SaveFile.CurrentVersion;
            data.SavedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tmpPath = _path + ".tmp";
            var tmpHashPath = _hashPath + ".tmp";
            File.WriteAllText(tmpPath, json, new UTF8Encoding(false));
            File.WriteAllText(tmpHashPath, ComputeHash(json), new UTF8Encoding(false));

            if (File.Exists(_path))
            {
                // 旧主档即将退位为备份：先把它的校验文件一起转过去。
                if (File.Exists(_hashPath))
                {
                    DeleteIfExists(_backupHashPath);
                    File.Move(_hashPath, _backupHashPath);
                }

                File.Replace(tmpPath, _path, _backupPath);
                File.Move(tmpHashPath, _hashPath);
            }
            else
            {
                File.Move(tmpPath, _path);
                File.Move(tmpHashPath, _hashPath);
            }
        }

        public void Delete()
        {
            DeleteIfExists(_path);
            DeleteIfExists(_backupPath);
            DeleteIfExists(_hashPath);
            DeleteIfExists(_backupHashPath);
        }

        private SaveFile TryLoad(string path, string hashPath)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path, Encoding.UTF8);
                if (json.Length == 0)
                {
                    return null;
                }

                if (!VerifyHash(json, hashPath))
                {
                    Debug.LogWarning("[Save] Checksum mismatch for " + path);
                    return null;
                }

                return JsonConvert.DeserializeObject<SaveFile>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Failed to read " + path + ": " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// [刻意放行] 没有 .sha256 时返回 true：兼容旧档/手动放置的存档，
        /// 不让玩家因为缺一个校验文件就打不开游戏。有 hash 时才严格比对。
        /// </summary>
        private static bool VerifyHash(string json, string hashPath)
        {
            if (!File.Exists(hashPath))
            {
                // 旧档或首次写入：没有校验文件就不阻断。
                return true;
            }

            var expected = File.ReadAllText(hashPath, Encoding.UTF8).Trim();
            return string.Equals(expected, ComputeHash(json), StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHash(string text)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
