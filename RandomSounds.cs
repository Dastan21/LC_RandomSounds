using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RandomSounds
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class RandomSounds : BaseUnityPlugin
    {
        public const string RandomSoundsDir = "RandomSounds";
        public const string SeedRPCSignature = "RCPS_SeedSync";
        public const string OriginalKey = "original";
        public static readonly string[] AllowedExtensions = { ".wav", ".mp3", ".ogg" };

        public static RandomSounds Instance;
        public static int Seed = new System.Random().Next();
        public static int SeedOffset = 0;
        public static System.Random random = new System.Random(Seed);

        internal ManualLogSource logger;

        public Dictionary<string, string> soundPacks = new Dictionary<string, string>();
        public static Dictionary<string, HashSet<ClipWeight>> ReplacedClips = new Dictionary<string, HashSet<ClipWeight>>();

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

        private void ProcessSoundFiles(string audioPath)
        {
            string[] files = Directory
                .GetFiles(audioPath)
                .Where(file => AllowedExtensions.Any(file.ToLower().EndsWith))
                .ToArray();
            if (files.Length <= 0) return;

            string audioName = Path.GetFileName(audioPath);
            string weightsPath = Path.Combine(audioPath, "weights.json");

            // Load sounds weights
            SoundWeight[] soundsWeights = new SoundWeight[0];
            int totalWeight = files.Length;
            if (File.Exists(weightsPath))
            {
                try
                {
                    soundsWeights = JsonConvert.DeserializeObject<SoundWeight[]>(File.ReadAllText(weightsPath));
                    totalWeight = soundsWeights.Aggregate(0, (t, sw) => t + Math.Max(sw.weight, 0));
                }
                catch (Exception e)
                {
                    logger.LogWarning($"Could not parse {audioName} weights.json \n{e}");
                }
            }

            int defaultWeight = totalWeight / files.Length;
            int weight = defaultWeight;
            foreach (string filePath in files)
            {
                weight = defaultWeight;
                AudioClip audioClip = GetAudioClip(filePath);
                string clipName = audioClip.GetName();
                try
                {
                    SoundWeight soundWeight = soundsWeights.First(sw => sw.sound == clipName);
                    weight = soundWeight.weight;
                }
                catch (Exception) { }
                AddAudioClip(audioName, audioClip, weight);
                logger.LogInfo($"Added {audioName}/{clipName} with weight {weight}");
            }

            // Default sound
            weight = defaultWeight;
            try
            {
                SoundWeight defaultSoundWeight = soundsWeights.First(sw => sw.sound == OriginalKey);
                weight = defaultSoundWeight.weight;
            }
            catch (Exception) { }
            AddDefaultAudioWeight(audioName, weight);
        }

        private static void AddAudioClip(string originalName, AudioClip newClip, int weight)
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
                ReplacedClips[originalName].Add(new ClipWeight(newClip, weight));
            }
            else
            {
                ReplacedClips.Add(originalName, new() { new ClipWeight(newClip, weight) });
            }
        }

        private void AddDefaultAudioWeight(string audioName, int weight)
        {
            logger.LogInfo($"Set original audio {audioName} weight to {weight}");
            if (ReplacedClips.ContainsKey(audioName))
            {
                ReplacedClips[audioName].Add(new ClipWeight(null, weight));
            }
            else
            {
                ReplacedClips.Add(audioName, new() { new ClipWeight(null, weight) });
            }
        }

        private static AudioClip GetAudioClip(string soundPath)
        {
            if (!File.Exists(soundPath))
            {
                Instance.logger.LogWarning($"Requested audio file does not exist at path {soundPath}!");
                return null;
            }

            return LoadClip(soundPath);
        }

        private static AudioClip LoadClip(string path)
        {
            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN))
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
}
