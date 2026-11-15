using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

public class AIProvider
{
    public string id { get; set; } = Guid.NewGuid().ToString("N");
    public string name { get; set; } = "未命名服务商";
    public string base_url { get; set; } = "";
    public string api_key { get; set; } = "";
    public string model { get; set; } = "";
    public double temperature { get; set; } = 0.3;
    public int? max_tokens { get; set; }
    public string note { get; set; } = "";
    public bool is_official { get; set; }
}

public static class AIProviderStore
{
    private static readonly object Locker = new object();
    public static List<AIProvider> Providers { get; private set; } = new List<AIProvider>();
    public static string ActiveId { get; private set; }

    public static string StoreDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalTasks");
    public static string StoreFile => Path.Combine(StoreDir, "ai_providers.json");

    public static readonly (string name, string url, string model)[] Presets =
    {
        ("Ollama", "http://localhost:11434/v1", "qwen2.5"),
        ("DeepSeek", "https://api.deepseek.com/v1", "deepseek-chat"),
        ("SiliconFlow", "https://api.siliconflow.cn/v1", "deepseek-ai/DeepSeek-V3")
    };

    public static void Load()
    {
        try
        {
            if (!File.Exists(StoreFile))
            {
                // first run: create official preset cards (not activated)
                Providers = Presets.Select(p => new AIProvider
                {
                    name = p.name,
                    base_url = p.url,
                    model = p.model,
                    is_official = true,
                    note = "内置预设模板"
                }).ToList();
                ActiveId = null;
                Save();
                return;
            }
            var o = JsonConvert.DeserializeObject<AISettingsFile>(File.ReadAllText(StoreFile));
            Providers = o?.providers ?? new List<AIProvider>();
            ActiveId = o?.active_id;
        }
        catch
        {
            Providers = new List<AIProvider>();
            ActiveId = null;
        }
    }

    public static void Save()
    {
        lock (Locker)
        {
            Directory.CreateDirectory(StoreDir);
            File.WriteAllText(StoreFile, JsonConvert.SerializeObject(
                new AISettingsFile { providers = Providers, active_id = ActiveId }, Formatting.Indented));
        }
    }

    public static AIProvider Active => Providers.FirstOrDefault(p => p.id == ActiveId);

    public static void SetActive(string id)
    {
        ActiveId = id;
        Save();
    }

    public static AIProvider Add(AIProvider p)
    {
        Providers.Add(p);
        Save();
        return p;
    }

    public static void Remove(string id)
    {
        Providers.RemoveAll(p => p.id == id);
        if (ActiveId == id) ActiveId = null;
        Save();
    }

    public static void Update(AIProvider p)
    {
        var i = Providers.FindIndex(x => x.id == p.id);
        if (i >= 0) Providers[i] = p;
        Save();
    }

    public static AIProvider Import(string name, string base_url, string api_key, string model,
        double? temperature = null, int? max_tokens = null, string note = null)
    {
        var p = new AIProvider
        {
            name = string.IsNullOrEmpty(name) ? "导入的服务商" : name,
            base_url = base_url ?? "",
            api_key = api_key ?? "",
            model = model ?? "",
            temperature = temperature ?? 0.3,
            max_tokens = max_tokens,
            note = note ?? "通过链接导入"
        };
        Providers.Add(p);
        Save();
        return p;
    }

    private class AISettingsFile
    {
        public List<AIProvider> providers { get; set; }
        public string active_id { get; set; }
    }
}
