using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace UnityFBXExporter {
    public class ExporterMenuMultiple : Editor {

        // Dropdown
        [MenuItem("GameObject/HiFi FBX Exporter/Only GameObject", false, 40)]
        public static void ExportDropdownGameObjectToFBX() {
            ExportCurrentGameObject(false, false);
        }

        [MenuItem("GameObject/HiFi FBX Exporter/With new Materials", false, 41)]
        public static void ExportDropdownGameObjectAndMaterialsToFBX() {
            ExportCurrentGameObject(true, false);
        }

        [MenuItem("GameObject/HiFi FBX Exporter/With new Materials and Textures", false, 42)]
        public static void ExportDropdownGameObjectAndMaterialsTexturesToFBX() {
            ExportCurrentGameObject(true, true);
        }

        // Assets
        [MenuItem("Assets/HiFi FBX Exporter/Only GameObject", false, 30)]
        public static void ExportGameObjectToFBX() {
            ExportCurrentGameObject(false, false);
        }

        [MenuItem("Assets/HiFi FBX Exporter/With new Materials", false, 31)]
        public static void ExportGameObjectAndMaterialsToFBX() {
            ExportCurrentGameObject(true, false);
        }

        [MenuItem("Assets/HiFi FBX Exporter/With new Materials and Textures", false, 32)]
        public static void ExportGameObjectAndMaterialsTexturesToFBX() {
            ExportCurrentGameObject(true, true);
        }

        private static void ExportCurrentGameObject(bool copyMaterials, bool copyTextures) {
            List<GameObject> currentGameObjects = new List<GameObject>();
            GameObject currentGameObject;

            // Export the entire scene if no object selected
            if (Selection.activeGameObject == null) {
                if (EditorUtility.DisplayDialog("Export Scene", "No Game Object Selected. Do you want to export the entire scene?", "OK", "Cancel")) {
                    var gameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (var obj in gameObjects) {
                        if (obj.activeInHierarchy) {
                            currentGameObjects.Add(obj);
                        }
                    }
                } else {
                    return;
                }
            } else {
                foreach (var obj in Selection.objects) {
                    currentGameObject = obj as GameObject;
                    if (currentGameObject == null) {
                        EditorUtility.DisplayDialog("Warning", "Item selected is not a GameObject", "Okay");
                        return;
                    } else {
                        currentGameObjects.Add(currentGameObject);
                    }
                }
            }
            //int id = 0;
            //int parentID = 0;
            //List<GameObject> currentGameObjects1 = new List<GameObject>();
            //foreach (var obj in currentGameObjects)
            //{
            //    var cloneObj = Instantiate(obj);
            //    currentGameObjects1.Add(cloneObj);
            //}
            //    foreach (var obj in currentGameObjects1) {
            //    Debug.Log("ObjName");
            //    Debug.Log(obj.name);
            //    Transform transform = obj.transform;
            //    //obj.tag = id.ToString();
            //    //parentID = id;
            //    foreach(Transform child in transform)
            //    {
            //        //child.tag = parentID.ToString();
            //        Debug.Log("ChildName");
            //        Debug.Log(child.name);
            //    }
            //    //transform.DetachChildren();
            //    //++id;
            //}
            string path = ExportGameObject(currentGameObjects, copyMaterials, copyTextures);
            if (path == null) {
                return;
            }
            ExportGameObjectAsJson(currentGameObjects, path);

            EditorUtility.DisplayDialog("Success", "Success " + currentGameObjects.Count + " game objects exported", "Okay");
        }

        /// <summary>
        /// Exports ANY Game Object given to it. Will provide a dialog and return the path of the newly exported file
        /// </summary>
        /// <returns>The path of the newly exported FBX file</returns>
        /// <param name="gameObj">Game object to be exported</param>
        /// <param name="copyMaterials">If set to <c>true</c> copy materials.</param>
        /// <param name="copyTextures">If set to <c>true</c> copy textures.</param>
        /// <param name="oldPath">Old path.</param>
        public static string ExportGameObject(List<GameObject> gameObjects, bool copyMaterials, bool copyTextures, string oldPath = null) {
            foreach (var gameObj in gameObjects)
            {
                if (gameObj == null)
                {
                    EditorUtility.DisplayDialog("Object is null", "Please select any GameObject to Export to FBX", "Okay");
                    return null;
                }
            }
            // Get folder path
            string newPath = GetNewPath(oldPath);
            if(newPath == null) {
                return null;
            } 
            foreach (var gameObject in gameObjects) {
                Transform transform = gameObject.transform;
                if (transform.parent) {
                    continue;
                }
                var fileName = newPath + "/" + gameObject.name + ".fbx";
                if (fileName != null && fileName.Length != 0) {
                    bool isSuccess = FBXExporter.ExportGameObjToFBX(gameObject, fileName, copyMaterials, copyTextures);
                    if (!isSuccess) {
                        EditorUtility.DisplayDialog("Warning", "The extension probably wasn't an FBX file, could not export.", "Okay");
                    } 
                }
            }
            return newPath;
        }

        /// <summary>
        /// Creates save dialog window depending on old path or right to the /Assets folder no old path is given
        /// </summary>
        /// <returns>The new path.</returns>
        /// <param name="gameObject">Item to be exported</param>
        /// <param name="oldPath">The old path that this object was original at.</param>
        private static string GetNewPath(string oldPath = null) {
            // NOTE: This must return a path with the starting "Assets/" or else textures won't copy right
            string newPath = null;

            if (oldPath == null) {
                newPath = EditorUtility.SaveFolderPanel("Select Folder to Export FBX", "/Assets", "");
                if (!newPath.Contains("/Assets")) {
                    EditorUtility.DisplayDialog("Warning", "Must save file in the project's assets folder", "Okay");
                    return null;
                }
            } else {
                if (oldPath.StartsWith("/Assets")) {
                    oldPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/Assets"), 7) + oldPath;
                    oldPath = oldPath.Remove(oldPath.LastIndexOf('/'), oldPath.Length - oldPath.LastIndexOf('/'));
                }
                newPath = EditorUtility.SaveFolderPanel("Select Folder to Export FBX", oldPath, "");
            }

            int assetsIndex = newPath.IndexOf("Assets");

            if (assetsIndex < 0)
                return null;

            if (assetsIndex > 0)
                newPath = newPath.Remove(0, assetsIndex);

            return newPath;
        }

        public static void ExportGameObjectAsJson(List<GameObject> gameObjects, string path) {
            Vector3 nullVector = new Vector3(0, 0, 0);
            Vector3 unitVector = new Vector3(1, 1, 1);

            string filePath = EditorUtility.SaveFilePanelInProject("Select JSON Filename", "gameObjects.json", "json", "Export GameObjects to a JSON file");

            StringBuilder jsonOutput = new StringBuilder("{\"Entities\":[");

            for (int i = 0; i < gameObjects.Count; i++) {
                SerializeJSON jsonObject = new SerializeJSON();
                Transform transform = gameObjects[i].transform;
                if (transform.parent) {
                    continue;
                }
                jsonObject.position = gameObjects[i].transform.position;
                jsonObject.position.x *= -1;

                //jsonObject.rotation = gameObjects[i].transform.localEulerAngles;
                //jsonObject.rotation.y *= -1;
                //jsonObject.rotation.z *= -1;

                if (gameObjects[i].GetComponent<MeshFilter>()) {
                    Mesh mesh = gameObjects[i].GetComponent<MeshFilter>().mesh;
                    Vector3 minBound = mesh.bounds.min;
                    Vector3 boundSize = mesh.bounds.size;
                    jsonObject.registrationPoint.x = (minBound.x * -1) / boundSize.x;
                    jsonObject.registrationPoint.y = (minBound.y * -1) / boundSize.y;
                    jsonObject.registrationPoint.z = (minBound.z * -1) / boundSize.z;
                }

                if (gameObjects[i].GetComponent<Renderer>()) {
                    Bounds bounds = gameObjects[i].GetComponent<Renderer>().bounds;
                    Renderer[] renderers = gameObjects[i].GetComponentsInChildren<Renderer>();

                    foreach (Renderer renderer in renderers) {
                        bounds.Encapsulate(renderer.bounds);
                    }

                    jsonObject.dimensions = bounds.size;
                    jsonObject.dimensions = Vector3.Scale(jsonObject.dimensions, gameObjects[i].transform.localScale);
                } else if (gameObjects[i].GetComponent<Collider>()) {
                    Bounds bounds = gameObjects[i].GetComponent<Collider>().bounds;
                    Collider[] colliders = gameObjects[i].GetComponentsInChildren<Collider>();

                    foreach (Collider collider in colliders) {
                        bounds.Encapsulate(collider.bounds);
                    }

                    jsonObject.dimensions = bounds.size;
                    jsonObject.dimensions = Vector3.Scale(jsonObject.dimensions, gameObjects[i].transform.localScale);
                }

                //if (gameObjects[i].GetComponent<MeshFilter>())
                //{
                //    Mesh mesh = gameObjects[i].GetComponent<MeshFilter>().mesh;
                //    jsonObject.dimensions = mesh.bounds.size;
                //    jsonObject.dimensions = Vector3.Scale(jsonObject.dimensions, gameObjects[i].transform.localScale);
                //}

                if (jsonObject.dimensions == nullVector) {
                    jsonObject.dimensions = unitVector;
                }
                jsonObject.type = "Model";
                string directory = Application.dataPath.Replace("Assets", "");
                jsonObject.modelURL = "file:///" + directory + path + "/" + gameObjects[i].name + ".fbx";

                if (gameObjects[i].GetComponent<Light>()) {
                    jsonObject.type = "Light";
                } else if (gameObjects[i].GetComponent<Camera>()) {
                    jsonObject.type = "";
                }
                //else if (gameObjects[i].GetComponent<MeshFilter>()) {
                //    var mesh = gameObjects[i].GetComponent<MeshFilter>().sharedMesh;
                //    switch (mesh.name) {
                //        case "Cube Instance":
                //            jsonObject.type = "Box";
                //            break;
                //        case "Sphere Instance":
                //            jsonObject.type = "Sphere";
                //            break;
                //    }
                //}

                string jsonString = JsonUtility.ToJson(jsonObject);
                jsonOutput.Append(jsonString);
                if (i != gameObjects.Count - 1) {
                    jsonOutput.Append(",");
                }
            }
            jsonOutput.Append("]}");
            System.IO.File.WriteAllText(filePath, jsonOutput.ToString());
        }
    }

}
