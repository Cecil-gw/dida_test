using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

public class AIChatMessage
{
    public string role { get; set; }
    public string content { get; set; }
}

public class AIConversation
{
    public string id { get; set; } = Guid.NewGuid().ToString("N");
    public string title { get; set; } = "新会话";
    public DateTime created_at { get; set; } = DateTime.Now;
    public List<AIChatMessage> messages { get; set; } = new List<AIChatMessage>();
}

public static class AIConversationStore
{
    public static List<AIConversation> Conversations { get; private set; } = new List<AIConversation>();
    public static string ActiveId { get; set; }

    private static string StoreFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalTasks", "ai_conversations.json");

    public static void Load()
    {
        try
        {
            if (!File.Exists(StoreFile)) return;
            Conversations = JsonConvert.DeserializeObject<List<AIConversation>>(File.ReadAllText(StoreFile)) ?? new List<AIConversation>();
        }
        catch
        {
            Conversations = new List<AIConversation>();
        }
    }

    public static AIConversation StartNew()
    {
        var c = new AIConversation();
        Conversations.Insert(0, c);
        ActiveId = c.id;
        Save();
        return c;
    }

    public static AIConversation Current()
    {
        if (ActiveId == null) return StartNew();
        return Conversations.FirstOrDefault(c => c.id == ActiveId) ?? StartNew();
    }

    public static void SyncFromHistory(List<(string role, string content)> history)
    {
        var cur = Current();
        cur.messages = history.Select(h => new AIChatMessage { role = h.role, content = h.content }).ToList();
        if (history.Count > 0 && cur.title == "新会话")
        {
            cur.title = history[0].content.Length > 24 ? history[0].content.Substring(0, 24) + "…" : history[0].content;
        }
        Save();
    }

    public static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StoreFile));
        File.WriteAllText(StoreFile, JsonConvert.SerializeObject(Conversations, Formatting.Indented));
    }
}
