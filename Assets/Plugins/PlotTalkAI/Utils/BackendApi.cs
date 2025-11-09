using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

namespace Plugins.PlotTalkAI.Utils
{
    public static class BackendApi
    {
        private const string BASE_URL = "https://plottalkai-backend.onrender.com";

        [System.Serializable]
        private class LoginRequest
        {
            public string mail;
            public string password;
        }
        
        [System.Serializable]
        private class RegisterRequest
        {
            public string mail;
            public string name;
            public string surname;
            public string password;
        }

        private static string currentToken = "";

        public static void Login(string email, string password, Action<bool, string, JObject> callback)
        {
            var requestObj = new LoginRequest
            {
                mail = email,
                password = password
            };

            string jsonData = JsonUtility.ToJson(requestObj);
            SendPostRequest("/api/login", jsonData, false, (success, response) =>
            {
                if (success && response != null)
                {
                    var token = response["access_token"]?.ToString();
                    var userData = response["user"] as JObject;
                    
                    if (!string.IsNullOrEmpty(token) && userData != null)
                    {
                        currentToken = token;
                        
                        // Получаем дополнительные данные пользователя
                        GetUserData(token, (dataSuccess, userDataResponse) =>
                        {
                            if (dataSuccess)
                            {
                                // Объединяем базовые данные пользователя с дополнительными
                                userData.Merge(userDataResponse, new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace,
                                    MergeNullValueHandling = MergeNullValueHandling.Merge
                                });
                                
                                callback(true, token, userData);
                            }
                            else
                            {
                                callback(false, "Не удалось получить данные пользователя", null);
                            }
                        });
                    }
                    else
                    {
                        callback(false, "Неправильный формат ответа", null);
                    }
                }
                else
                {
                    if (response?["detail"] != null)
                    {
                        callback(false, response["detail"].ToString(), null);
                    }
                    else
                    {
                        callback(false, "Ошибка авторизации", null);
                    }
                }
            });
        }
        
        public static void GetUserDataFromServer(Action<bool, JObject> callback)
        {
            var token = GetCurrentToken();
            if (string.IsNullOrEmpty(token))
            {
                callback(false, new JObject { ["message"] = "No authentication token available" });
                return;
            }

            var url = BASE_URL + "/api/users/me/data";
            var request = UnityWebRequest.Get(url);
            
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("Content-Type", "application/json");
            
            var operation = request.SendWebRequest();
            
            operation.completed += (asyncOp) =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;
                        JObject responseJson = JObject.Parse(responseText);
                        callback(true, responseJson);
                    }
                    else
                    {
                        string responseText = request.downloadHandler.text;
                        Debug.LogError($"Get user data failed: {request.error}, Response: {responseText}");
                        
                        JObject errorResponse = null;
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            try
                            {
                                errorResponse = JObject.Parse(responseText);
                            }
                            catch
                            {
                                errorResponse = new JObject
                                {
                                    ["message"] = responseText
                                };
                            }
                        }
                        else
                        {
                            errorResponse = new JObject
                            {
                                ["message"] = request.error
                            };
                        }
                        callback(false, errorResponse);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Get user data error: {e.Message}");
                    callback(false, new JObject { ["message"] = e.Message });
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        public static void Register(string email, string name, string surname, string password, Action<bool, string> callback)
        {
            var requestObj = new RegisterRequest
            {
                mail = email,
                name = name,
                surname = surname,
                password = password
            };

            string jsonData = JsonUtility.ToJson(requestObj);
            SendPostRequest("/api/register", jsonData, false, (success, response) =>
            {
                if (success)
                {
                    callback(true, "Регистрация прошла успешно");
                }
                else
                {
                    callback(false, response?["message"]?.ToString() ?? "Регистрация не удалась");
                }
            });
        }

        private static void GetUserData(string token, Action<bool, JObject> callback)
        {
            var url = BASE_URL + "/api/users/me/data";
            var request = UnityWebRequest.Get(url);
            
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("Content-Type", "application/json");
            
            var operation = request.SendWebRequest();
            
            operation.completed += (asyncOp) =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;
                        JObject responseJson = JObject.Parse(responseText);
                        callback(true, responseJson);
                    }
                    else
                    {
                        Debug.LogError($"Failed to get user data: {request.error}");
                        // Если endpoint не существует или возвращает ошибку, возвращаем пустой объект с играми
                        var emptyData = new JObject
                        {
                            ["games"] = new JArray()
                        };
                        callback(true, emptyData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"GetUserData error: {e.Message}");
                    // В случае ошибки возвращаем пустой объект с играми
                    var emptyData = new JObject
                    {
                        ["games"] = new JArray()
                    };
                    callback(true, emptyData);
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        // Метод для обновления данных пользователя с использованием PUT
        public static void UpdateUserData(JObject userData, Action<bool, JObject> callback)
        {
            var dataToSend = new JObject
            {
                ["data"] = userData
            };

            string jsonData = dataToSend.ToString();
            
            // Используем PUT метод
            SendPutRequest("/api/users/me/upd/data", jsonData, (success, response) =>
            {
                if (success)
                {
                    callback(true, response);
                }
                else
                {
                    Debug.LogError($"Failed to update data on server: {response?["message"]?.ToString() ?? "Unknown error"}");
                    callback(false, response);
                }
            });
        }

        private static void SendPutRequest(string endpoint, string jsonData, Action<bool, JObject> callback)
        {
            var url = BASE_URL + endpoint;
            var request = new UnityWebRequest(url, "PUT");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // Обязательно добавляем заголовок Authorization
            if (!string.IsNullOrEmpty(currentToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {currentToken}");
            }
            else
            {
                Debug.LogError("No token available for authorization");
                callback(false, new JObject { ["message"] = "No authentication token available" });
                return;
            }
            
            var operation = request.SendWebRequest();
            
            operation.completed += (asyncOp) =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;
                        JObject responseJson = string.IsNullOrEmpty(responseText) ? new JObject() : JObject.Parse(responseText);
                        callback(true, responseJson);
                    }
                    else
                    {
                        string responseText = request.downloadHandler.text;
                        Debug.LogError($"PUT request failed: {request.error}, Response: {responseText}");
                        
                        JObject errorResponse = null;
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            try
                            {
                                errorResponse = JObject.Parse(responseText);
                            }
                            catch
                            {
                                errorResponse = new JObject
                                {
                                    ["message"] = responseText
                                };
                            }
                        }
                        else
                        {
                            errorResponse = new JObject
                            {
                                ["message"] = request.error
                            };
                        }
                        callback(false, errorResponse);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"PUT API request error: {e.Message}");
                    callback(false, new JObject { ["message"] = e.Message });
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        private static void SendPostRequest(string endpoint, string jsonData, bool useAuth, Action<bool, JObject> callback)
        {
            var url = BASE_URL + endpoint;
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // Добавляем заголовок Authorization если требуется
            if (useAuth && !string.IsNullOrEmpty(currentToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {currentToken}");
            }
            
            var operation = request.SendWebRequest();
            
            operation.completed += (asyncOp) =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;
                        JObject responseJson = JObject.Parse(responseText);
                        callback(true, responseJson);
                    }
                    else
                    {
                        JObject errorResponse = null;
                        if (!string.IsNullOrEmpty(request.downloadHandler.text))
                        {
                            try
                            {
                                errorResponse = JObject.Parse(request.downloadHandler.text);
                            }
                            catch
                            {
                                errorResponse = new JObject
                                {
                                    ["message"] = request.downloadHandler.text
                                };
                            }
                        }
                        else
                        {
                            errorResponse = new JObject
                            {
                                ["message"] = request.error
                            };
                        }
                        callback(false, errorResponse);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"API request error: {e.Message}");
                    callback(false, new JObject { ["message"] = e.Message });
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        public static string GetCurrentToken()
        {
            return currentToken;
        }

        public static void SetCurrentToken(string token)
        {
            currentToken = token;
        }

        public static void ClearToken()
        {
            currentToken = "";
        }
        
        public static void GenerateDialogue(JObject requestData, Action<bool, JObject> callback)
        {
            var url = BASE_URL + "/api/generate";
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData.ToString());
            
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(currentToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {currentToken}");
            }
            
            var operation = request.SendWebRequest();
            
            operation.completed += (asyncOp) =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;
                        JObject responseJson = JObject.Parse(responseText);
                        callback(true, responseJson);
                    }
                    else
                    {
                        string responseText = request.downloadHandler.text;
                        Debug.LogError($"Generate dialogue failed: {request.error}, Response: {responseText}");
                        
                        JObject errorResponse = null;
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            try
                            {
                                errorResponse = JObject.Parse(responseText);
                            }
                            catch
                            {
                                errorResponse = new JObject
                                {
                                    ["message"] = responseText
                                };
                            }
                        }
                        else
                        {
                            errorResponse = new JObject
                            {
                                ["message"] = request.error
                            };
                        }
                        callback(false, errorResponse);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Generate dialogue error: {e.Message}");
                    callback(false, new JObject { ["message"] = e.Message });
                }
                finally
                {
                    request.Dispose();
                }
            };
        }
    }
}