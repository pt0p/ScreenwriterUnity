using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Plugins.PlotTalkAI.Utils
{
    public class StorageApi
    {
        private static StorageApi _instance;
        private static string _jsonDir;
        private static string _jsonPath;

        private StorageApi()
        {
        }

        public static StorageApi GetInstance()
        {
            _jsonDir ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PlotTalkAI");
            _jsonPath ??= Path.Combine(_jsonDir, "users.json");
            return _instance ??= new StorageApi();
        }

        public static JObject Deserialize(string jsonString)
        {
            try
            {
                return JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse JSON: {e.Message}");
                return null;
            }
        }

        public static string Serialize(JObject jsonObject)
        {
            return jsonObject.ToString(Formatting.Indented);
        }

        public static T GetValue<T>(JObject jsonObject, string path, T defaultValue = default)
        {
            try
            {
                JToken token = jsonObject.SelectToken(path);
                return token != null ? token.ToObject<T>() : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static void SetValue(JObject jsonObject, string path, object value)
        {
            try
            {
                JToken token = jsonObject.SelectToken(path);
                if (token != null)
                {
                    token.Replace(JToken.FromObject(value));
                }
                else
                {
                    AddValue(jsonObject, path, value);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set value: {e.Message}");
            }
        }

        private static void AddValue(JObject jsonObject, string path, object value)
        {
            string[] parts = path.Split('.');
            JToken current = jsonObject;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (current[parts[i]] == null)
                {
                    current[parts[i]] = new JObject();
                }

                current = current[parts[i]];
            }

            current[parts[^1]] = JToken.FromObject(value);
        }

        public string GetDataString()
        {
            CheckAndCreateJsonInProgramData();
            return File.ReadAllText(_jsonPath);
        }

        public void SetDataString(string dataString)
        {
            CheckAndCreateJsonInProgramData();
            File.WriteAllText(_jsonPath, dataString);
        }

        public JToken GetUser()
        {
            var jsonString = GetDataString();
            var jsonObject = Deserialize(jsonString);
            return jsonObject.SelectToken("$.user");
        }

        public bool IsLoggedIn()
        {
            return GetUser().SelectToken("$.id") != null;
        }

        public void LogOut()
        {
            SetDataString("{ \"user\": {} }");
        }

        public void LogIn(int userId, string userToken, string userDataString)
        {
            if (IsLoggedIn())
            {
                Debug.LogError("User already logged in");
            }

            SetDataString("{ \"user\": {\"id\":" + userId + ",\"token\":\"" + userToken + "\",\"data\":" +
                          userDataString + "} }");
        }

        public void CreateGame()
        {
        }

        public void CheckAndCreateJsonInProgramData()
        {
            // создаём подпапку для плагина
            if (!Directory.Exists(_jsonDir))
            {
                Directory.CreateDirectory(_jsonDir);
            }

            if (!File.Exists(_jsonPath))
            {
                // тестовое содержимое
                string json = "{ \"user\": {} }";

                File.WriteAllText(_jsonPath, json);
            }
        }

        public void AddGame(JObject game)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);
            games.Add(game);
            SaveFullJson(fullJson);
        }

        public void UpdateGame(string gameId, JObject updatedGame)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            foreach (JObject game in games.Where(g => g["id"]?.ToString() == gameId))
            {
                game.Merge(updatedGame, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                SaveFullJson(fullJson);
                return;
            }

            Debug.LogError($"Game with id {gameId} not found");
        }

        public void DeleteGame(string gameId)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject gameToRemove = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (gameToRemove != null)
            {
                games.Remove(gameToRemove);
                SaveFullJson(fullJson);
                return;
            }

            Debug.LogError($"Game with id {gameId} not found");
        }

        public void AddScene(string gameId, JObject scene)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray scenes = GetScenesArray(game);
            scenes.Add(scene);
            SaveFullJson(fullJson);
        }

        public void UpdateScene(string gameId, long sceneId, JObject updatedScene)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray scenes = GetScenesArray(game);

            foreach (JObject scene in scenes.Where(g => (long)g["id"] == sceneId))
            {
                scene.Merge(updatedScene, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                SaveFullJson(fullJson);
                return;
            }

            Debug.LogError($"Scene with id {sceneId} not found in game {gameId}");
        }

        public void DeleteScene(string gameId, long sceneId)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray scenes = GetScenesArray(game);

            JObject sceneToRemove = scenes.FirstOrDefault(s => (long)s["id"] == sceneId) as JObject;
            if (sceneToRemove != null)
            {
                scenes.Remove(sceneToRemove);
                SaveFullJson(fullJson);
                return;
            }

            Debug.LogError($"Scene with id {sceneId} not found in game {gameId}");
        }

        public void AddScript(string gameId, long sceneId, JObject script)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray scenes = GetScenesArray(game);
            JObject scene = scenes.FirstOrDefault(s => (long)s["id"] == sceneId) as JObject;

            if (scene == null)
            {
                Debug.LogError($"Scene with id {sceneId} not found in game {gameId}");
                return;
            }

            JArray scripts = GetScriptsArray(scene);
            scripts.Add(script);
            SaveFullJson(fullJson);
        }

        public void UpdateScript(string gameId, long sceneId, string scriptId, JObject updatedScript)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray scenes = GetScenesArray(game);
            JObject scene = scenes.FirstOrDefault(s => (long)s["id"] == sceneId) as JObject;

            if (scene == null)
            {
                Debug.LogError($"Scene with id {sceneId} not found in game {gameId}");
                return;
            }

            JArray scripts = GetScriptsArray(scene);

            foreach (JObject script in scripts.Where(g => g["id"]?.ToString() == scriptId))
            {
                script.Merge(updatedScript, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                SaveFullJson(fullJson);
                return;
            }

            Debug.LogError($"Script with id {scriptId} not found in scene {sceneId}");
        }

        public void DeleteScript(string gameId, long sceneId, string scriptId)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray scenes = GetScenesArray(game);
            JObject scene = scenes.FirstOrDefault(s => (long)s["id"] == sceneId) as JObject;

            if (scene == null)
            {
                Debug.LogError($"Scene with id {sceneId} not found in game {gameId}");
                return;
            }

            JArray scripts = GetScriptsArray(scene);

            JObject scriptToRemove = scripts.FirstOrDefault(s => s["id"]?.ToString() == scriptId) as JObject;
            if (scriptToRemove != null)
            {
                scripts.Remove(scriptToRemove);
                SaveFullJson(fullJson);
                return;
            }

            Debug.LogError($"Script with id {scriptId} not found in scene {sceneId}");
        }

        // Вспомогательные методы
        public JObject LoadFullJson()
        {
            return Deserialize(GetDataString());
        }

        private void SaveFullJson(JObject json)
        {
            SetDataString(Serialize(json));
        }

        public JArray GetGamesArray(JObject fullJson)
        {
            if (fullJson["user"]?["data"]?["games"] == null)
            {
                fullJson["user"]["data"]["games"] = new JArray();
            }

            return fullJson["user"]["data"]["games"] as JArray;
        }

        public JObject GetGameById(string gameId)
        {
            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);
            return games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
        }

        public JArray GetScenesArray(JObject game)
        {
            if (game["scenes"] == null)
            {
                game["scenes"] = new JArray();
            }

            return game["scenes"] as JArray;
        }

        public JObject GetSceneById(string gameId, long sceneId)
        {
            JObject game = GetGameById(gameId);
            if (game == null) return null;

            JArray scenes = GetScenesArray(game);
            return scenes.FirstOrDefault(s => (long)s["id"] == sceneId) as JObject;
        }
        
        public JObject GetScriptById(string gameId, long sceneId, string scriptId)
        {
            JObject game = GetGameById(gameId);
            if (game == null) return null;

            JObject scene = GetSceneById(gameId, sceneId);
            if (scene == null) return null;
            
            JArray scripts = GetScriptsArray(scene);
            return scripts.FirstOrDefault(s => (string)s["id"] == scriptId) as JObject;
        }

        public JArray GetScriptsArray(JObject scene)
        {
            if (scene["scripts"] == null)
            {
                scene["scripts"] = new JArray();
            }

            return scene["scripts"] as JArray;
        }

        public void AddCharacter(string gameId, JObject character)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray characters = GetCharactersArray(game);
            characters.Add(character);
            SaveFullJson(fullJson);
        }

        public void UpdateCharacter(string gameId, string characterId, JObject updatedCharacter)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray characters = GetCharactersArray(game);

            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i]["id"]?.ToString() == characterId)
                {
                    characters[i] = updatedCharacter;
                    SaveFullJson(fullJson);
                    return;
                }
            }

            Debug.LogError($"Character with id {characterId} not found in game {gameId}");
        }

        public void DeleteCharacter(string gameId, string characterId)
        {
            if (!IsLoggedIn())
            {
                Debug.LogError("User must be logged in");
                return;
            }

            JObject fullJson = LoadFullJson();
            JArray games = GetGamesArray(fullJson);

            JObject game = games.FirstOrDefault(g => g["id"]?.ToString() == gameId) as JObject;
            if (game == null)
            {
                Debug.LogError($"Game with id {gameId} not found");
                return;
            }

            JArray characters = GetCharactersArray(game);

            JObject characterToRemove = characters.FirstOrDefault(c => c["id"]?.ToString() == characterId) as JObject;
            if (characterToRemove != null)
            {
                characters.Remove(characterToRemove);
                SaveFullJson(fullJson);
                return;
            }

            Debug.LogError($"Character with id {characterId} not found in game {gameId}");
        }

        public JArray GetCharactersArray(JObject game)
        {
            if (game["characters"] == null)
            {
                game["characters"] = new JArray();
            }

            return game["characters"] as JArray;
        }
    }
}