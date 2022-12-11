using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelBuilder : EditorWindow
{
    private string _path = "Assets/Editor Resources";
    private string[] _paths;

    private Vector2 _scrollPosition;
    private int _selectedElement;
    private int _previewElement;
    private List<GameObject> _catalog = new List<GameObject>();
    private bool _building;

    private GameObject _createdObject;
    private GameObject _previewObject;
    private GameObject _parent;

    private Vector3 _currentObjectsScale = new Vector3(1, 1, 1);

    private Vector3 _yRotationAngle = new Vector3(0, 10, 0);
    private Vector3 _scaleStep = new Vector3(1, 1, 1);

    private int _selectedTabNumber = 0;
    private string[] _tabNames = { "Building", "Tree" };

    [MenuItem("Level/Builder")]
    private static void ShowWindow()
    {
        GetWindow(typeof(LevelBuilder));
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        RefreshTab();
        RefreshCatalog(_selectedTabNumber);
    }

    private void OnGUI()
    {
        _parent = (GameObject)EditorGUILayout.ObjectField("Parent", _parent, typeof(GameObject), true);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Preview object settings");
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Use 'A' and 'D' to rotate the dragging object\nUse 'W' and 'S' to change the scale of the dragging object", MessageType.None, true);
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        _scaleStep = EditorGUILayout.Vector3Field("Scale Step", _scaleStep);
        EditorGUILayout.EndHorizontal();
        _yRotationAngle.y = EditorGUILayout.FloatField("Y Rotation Angle", _yRotationAngle.y);

        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        _building = GUILayout.Toggle(_building, "Start building", "Button", GUILayout.Height(60));

        if (_parent == null)
            _building = false;

        if (_building == false)
            DestroyImmediate(_previewObject);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        _selectedTabNumber = GUILayout.Toolbar(_selectedTabNumber, _tabNames);

        EditorGUILayout.BeginVertical(GUI.skin.window);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        RefreshCatalog(_selectedTabNumber);
        DrawCatalog(GetCatalogIcons());

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        TryToRotateObject();
        TryToScaleObject();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_building)
        {
            if (Raycast(out Vector3 contactPoint))
            {
                if (_previewObject == null || _previewElement != _selectedElement)
                    ShowPreviewObject(contactPoint);

                TryToRotateObject();
                TryToScaleObject();

                DrawPreviewObject(contactPoint, Color.red);

                if (CheckInput())
                {
                    PutPreviewObject();
                }

                sceneView.Repaint();
            }
        }
    }

    private bool Raycast(out Vector3 contactPoint)
    {
        Ray guiRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        contactPoint = Vector3.zero;

        if (Physics.Raycast(guiRay, out RaycastHit raycastHit))
        {
            contactPoint = raycastHit.point;
            return true;
        }

        return false;
    }

    private void TryToRotateObject()
    {
        if (CheckInputToLeftRotateObject() && _previewObject != null)
        {
            _previewObject.transform.localEulerAngles += _yRotationAngle;
        }

        if (CheckInputToRightRotateObject() && _previewObject != null)
        {
            _previewObject.transform.localEulerAngles -= _yRotationAngle;
        }
    }

    private void TryToScaleObject()
    {
        if (CheckInputToIncreaseScale() && _previewObject != null)
        {
            _previewObject.transform.localScale += _scaleStep;

            _currentObjectsScale = _previewObject.transform.localScale;
        }

        if (CheckInputToDecreaseScale() && _previewObject != null)
        {
            _previewObject.transform.localScale -= _scaleStep;

            if (_previewObject.transform.localScale.x < 0 || _previewObject.transform.localScale.y < 0 || _previewObject.transform.localScale.z < 0)
            {
                _previewObject.transform.localScale = Vector3.zero;
            }

            _currentObjectsScale = _previewObject.transform.localScale;
        }
    }

    private bool CheckInputToLeftRotateObject()
    {
        HandleUtility.AddDefaultControl(0);

        return Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.D;
    }

    private bool CheckInputToRightRotateObject()
    {
        HandleUtility.AddDefaultControl(0);

        return Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.A;
    }

    private bool CheckInputToIncreaseScale()
    {
        HandleUtility.AddDefaultControl(0);

        return Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W;
    }

    private bool CheckInputToDecreaseScale()
    {
        HandleUtility.AddDefaultControl(0);

        return Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S;
    }

    private void DrawPreviewObject(Vector3 position, Color color)
    {
        Handles.color = color;
        _previewObject.transform.position = position;
    }

    private bool CheckInput()
    {
        HandleUtility.AddDefaultControl(0);
        return Event.current.type == EventType.MouseDown && Event.current.button == 0 && !Event.current.alt;
    }

    private void ShowPreviewObject(Vector3 position)
    {
        DestroyImmediate(_previewObject);

        if (_selectedElement < _catalog.Count)
        {
            GameObject prefab = _catalog[_selectedElement];
            _previewObject = Instantiate(prefab);
            _previewObject.transform.position = position;
            _previewElement = _selectedElement;

            _previewObject.transform.localScale = _currentObjectsScale;
        }
    }

    private void PutPreviewObject()
    {
        _previewObject.AddComponent<BoxCollider>();
        var colliders = Physics.OverlapBox(_previewObject.GetComponent<BoxCollider>().transform.position, _previewObject.GetComponent<MeshRenderer>().bounds.extents, Quaternion.identity, 7);

        if (colliders.Length <= 1)
        {
            _createdObject = _previewObject;
            _createdObject.transform.parent = _parent.transform;
            Undo.RegisterCreatedObjectUndo(_createdObject, "Create Building");
            _previewObject = null;
        }
        else
        {
            Debug.Log("cannot be placed because they are nearby " + colliders.Length + " colliders");
            DestroyImmediate(_previewObject.GetComponent<BoxCollider>());
        }
    }

    private void RefreshTab()
    {
        var pathList = System.IO.Directory.GetDirectories(_path);
        _tabNames = new string[pathList.Length];
        _paths = new string[pathList.Length];

        for (int i = 0; i < pathList.Length; i++)
        {
            int position = pathList[i].LastIndexOf("\\");

            if (position != -1)
            {
                _tabNames[i] = pathList[i].Substring(position + 1);
                _paths[i] = pathList[i];
            }
        }
    }

    private void DrawCatalog(List<GUIContent> catalogIcons)
    {
        int minColumnsCount = 2;
        int iconWidth = 100;
        int iconHeigth = 100;

        if (catalogIcons.Count > 0 && catalogIcons[0].image)
        {
            iconWidth = catalogIcons[0].image.width;
            iconHeigth = catalogIcons[0].image.height;
        }

        int columnsCount = (int)position.width / iconWidth;

        if (columnsCount < minColumnsCount)
            columnsCount = minColumnsCount;

        int scrollWidth = iconWidth * columnsCount;
        int scrollHeigth = (catalogIcons.Count / columnsCount) * iconHeigth;

        _selectedElement = GUILayout.SelectionGrid(_selectedElement, catalogIcons.ToArray(), columnsCount, GUILayout.Width(scrollWidth), GUILayout.Height(scrollHeigth));
    }

    private List<GUIContent> GetCatalogIcons()
    {
        List<GUIContent> catalogIcons = new List<GUIContent>();

        foreach (var element in _catalog)
        {
            Texture2D texture = AssetPreview.GetAssetPreview(element);
            catalogIcons.Add(new GUIContent(texture));
        }

        return catalogIcons;
    }

    private void RefreshCatalog(int pathId)
    {
        _catalog.Clear();

        System.IO.Directory.CreateDirectory(_paths[pathId]);
        string[] prefabFiles = System.IO.Directory.GetFiles(_paths[pathId], "*.prefab");

        foreach (var prefabFile in prefabFiles)
            _catalog.Add(AssetDatabase.LoadAssetAtPath(prefabFile, typeof(GameObject)) as GameObject);
    }
}