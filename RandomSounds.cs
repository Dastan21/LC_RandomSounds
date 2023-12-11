using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace RandomSounds;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class RandomSounds : BaseUnityPlugin
{
    public const string RandomSoundsDir = "RandomSounds";
    public const string SeedRPCSignature = "RCPS_SeedSync";

    public static RandomSounds Instance;
    public static int Seed = new System.Random().Next();
    public static int SeedOffset = 0;
    public static System.Random random = new System.Random(Seed);

    internal ManualLogSource logger;

    public Dictionary<string, string> soundPacks = new Dictionary<string, string>();
    public static Dictionary<string, HashSet<AudioClip>> ReplacedClips = new Dictionary<string, HashSet<AudioClip>>();

    private Harmony harmony;

    private void Awake()
    {
        if (Instance != null) return;

        Instance = this;

        logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID);
        logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        CreateCustomSoundsFolder();

        LC_API.ServerAPI.Networking.GetString += GetSeedSync;
    }

    private void GetSeedSync(string data, string signature)
    {
        if (signature != SeedRPCSignature) return;

        string[] seedData = data.Split("_");
        try
        {
            int seed = int.Parse(seedData[0]);
            int seedOffset = int.Parse(seedData[1]);
            // sync seed
            if (seed != Seed || seedOffset != SeedOffset)
            {
                logger.LogInfo($"Received seed {seed} & offset {seedOffset} from host.");
                Seed = seed;
                SeedOffset = seedOffset;
                random = new System.Random(Seed);
                for (int i = 0; i < SeedOffset; i++) { random.Next(); }
            }
        }
        catch (Exception e)
        {
            logger.LogWarning($"Failed to parse seed data\n{e}");
        }
    }

    private void Start()
    {
        LoadSounds();

        GameObject go = new("RCPSPlayerJoin");
        go.AddComponent<RCPSPlayerJoin>();
        DontDestroyOnLoad(go);
    }

    private void CreateCustomSoundsFolder()
    {
        string path = Path.Combine(Path.GetDirectoryName(Info.Location), RandomSoundsDir);
        Directory.CreateDirectory(path);
    }

    public void LoadSounds()
    {
        string directoryName = Path.GetDirectoryName(Info.Location);
        string baseDir = Path.Combine(directoryName, RandomSoundsDir);
        if (Directory.Exists(baseDir))
        {
            string[] directories = Directory.GetDirectories(baseDir);
            foreach (string dir in directories)
            {
                ProcessSoundFiles(dir);
            }
        }
        else
        {
            logger.LogInfo($"{RandomSoundsDir} folder not found.");
        }
    }

    private void ProcessSoundFiles(string propPath)
    {
        string[] files = Directory.GetFiles(propPath, "*.wav");
        foreach (string filePath in files)
        {
            AudioClip audioClip = GetAudioClip(filePath);
            string propName = Path.GetFileName(propPath);
            AddAudioClip(propName, audioClip);
            logger.LogInfo($"Added {propName}/{audioClip.name}");
        }
    }

    public static void AddAudioClip(string originalName, AudioClip newClip)
    {
        if (string.IsNullOrEmpty(originalName))
        {
            Instance.logger.LogWarning($"Trying to replace an audio clip without original clip specified! This is not allowed.");
            return;
        }
        if (newClip == null)
        {
            Instance.logger.LogWarning($"Trying to replace an audio clip without new clip specified! This is not allowed.");
            return;
        }

        if (ReplacedClips.ContainsKey(originalName))
        {
            ReplacedClips[originalName].Add(newClip);
        }
        else
        {
            ReplacedClips.Add(originalName, new() { newClip });
        }
    }

    public static AudioClip GetAudioClip(string soundPath)
    {
        if (!File.Exists(soundPath))
        {
            Instance.logger.LogWarning($"Requested audio file does not exist at path {soundPath}!");
            return null;
        }

        Instance.logger.LogDebug($"Loading AudioClip from path: {soundPath}");
        return LoadClip(soundPath);
    }

    static AudioClip LoadClip(string path)
    {
        AudioClip clip = null;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV))
        {
            uwr.SendWebRequest();

            try
            {
                while (!uwr.isDone) { }

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Instance.logger.LogError($"Failed to load AudioClip from path: {path}\n{uwr.error}");

                }
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(uwr);
                    clip.name = Path.GetFileNameWithoutExtension(path);
                }
            }
            catch (Exception err)
            {
                Instance.logger.LogError($"{err.Message}, {err.StackTrace}");
            }
        }

        return clip;
    }
}
