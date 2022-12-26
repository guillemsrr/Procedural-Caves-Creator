using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelFloorsCreator : MonoBehaviour
{
    public static LevelFloorsCreator instance { get; private set; }

    [Header("Level Elements")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject cavePrefab;

    [Header("UI")]
    [SerializeField] private GameObject editorTools;
    [SerializeField] private Button randomSpecsButton;
    [SerializeField] private InputField minFloorsInput;
    [SerializeField] private InputField maxFloorsInput;
    [SerializeField] private InputField minHexsInput;
    [SerializeField] private InputField maxHexsInput;
    [SerializeField] private InputField minConnsInput;
    [SerializeField] private InputField maxConnsInput;
    [SerializeField] private Toggle fallingPath;
    [SerializeField] private Toggle gems;
    [SerializeField] private Toggle columns;
    [SerializeField] private Toggle rocks;
    [SerializeField] private InputField seedInput;
    [SerializeField] private Button randomSeed;
    [SerializeField] private Toggle infinite;
    [SerializeField] private InputField InfiniteTimer;
    [SerializeField] private Button generateLevelButton;


    //Variables
    private const int MIN_FLOORS = 1;
    private const int MAX_FLOORS = 5;
    private const int MIN_HEXS = 1;
    private const int MAX_HEXS = 200;
    private const int MIN_CONNS = 1;
    private const int MAX_CONNS = 5;

    private int numFloors;
    private int numHexs;
    private int numConns;
    private int seed;
    public const float FLOOR_HEIGHT = 25f;
    private List<GameObject> nextInitCaves;
    public List<GameObject> levelFloorsList;
    private List<SimpleHexLevel> levelFloorScriptsList;
    private bool infiniteLevels = false;
    private float time;
    private float maxTime;


    private void Awake()
    {
        instance = this;

        nextInitCaves = new List<GameObject>();
        levelFloorsList = new List<GameObject>();
        levelFloorScriptsList = new List<SimpleHexLevel>();
    }

    void Start()
    {
        randomSpecsButton.onClick.AddListener(RandomSpecs);
        randomSeed.onClick.AddListener(RandomSeed);
        generateLevelButton.onClick.AddListener(GenerateLevel);

        RandomSpecs();
    }
    
    void Update()
    {
        if (infiniteLevels)
        {
            time += Time.deltaTime;
            if (time > maxTime)
            {
                time = 0f;
                RandomSeed();
                GenerateLevel();
            }
        }
    }

    #region UI Listeners

    private void RandomSpecs()
    {
        int rnd = Random.Range(MIN_FLOORS, MAX_FLOORS);
        minFloorsInput.text = rnd.ToString();
        maxFloorsInput.text = rnd.ToString();
        rnd = Random.Range(MIN_HEXS, MAX_HEXS);
        minHexsInput.text = rnd.ToString();
        maxHexsInput.text = rnd.ToString();
        rnd = Random.Range(MIN_CONNS, MAX_CONNS);
        minConnsInput.text = rnd.ToString();
        maxConnsInput.text = rnd.ToString();
        RandomSeed();
    }

    private void RandomSeed()
    {
        seed = UnityEngine.Random.Range(0, System.Int32.MaxValue);
        seedInput.text = seed.ToString();
    }

    private void GenerateLevel()
    {
        editorTools.SetActive(false);

        //eliminate last level:
        while (levelFloorsList.Count != 0)
        {
            DestroyImmediate(levelFloorsList[0]);
            levelFloorsList.RemoveAt(0);
        }
        levelFloorScriptsList.Clear();

        while (nextInitCaves.Count != 0)
        {
            DestroyImmediate(nextInitCaves[0]);
            nextInitCaves.RemoveAt(0);
        }
        nextInitCaves.Clear();

        while(transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        Debug.Log("childs: " + transform.childCount);

        //new level
        GameObject firstCave = Instantiate(cavePrefab, transform);
        nextInitCaves.Add(firstCave);

        UnityEngine.Random.InitState(GetInt(seedInput));

        numFloors = UnityEngine.Random.Range(GetInt(minFloorsInput), GetInt(maxFloorsInput));
        numHexs = UnityEngine.Random.Range(GetInt(minHexsInput), GetInt(maxHexsInput));
        numConns = UnityEngine.Random.Range(GetInt(minConnsInput), GetInt(maxConnsInput));

        InputChecker();

        CreateFloorLevels();
        ConnectVerticalHexs();

        if(fallingPath.isOn)
            CreateFallingPathInFloor();
        if(rocks.isOn)
            GenerateRocksInFloor();


        infiniteLevels = infinite.isOn;
        maxTime = float.Parse(InfiniteTimer.text);

        editorTools.SetActive(true);
    }

    private int GetInt(InputField text)
    {
        return int.Parse(text.text.ToString(), System.Globalization.NumberStyles.Integer);
    }

    private void InputChecker()
    {
        int lastNum = numFloors;
        numFloors = Mathf.Clamp(numFloors, MIN_FLOORS, MAX_FLOORS);
        if(lastNum != numFloors)
        {
            minFloorsInput.text = numFloors.ToString();
            maxFloorsInput.text = numFloors.ToString();
        }

        lastNum = numHexs;
        numHexs = Mathf.Clamp(numHexs, MIN_HEXS, MAX_HEXS);
        if(lastNum != numHexs)
        {
            minHexsInput.text = numHexs.ToString();
            maxHexsInput.text = numHexs.ToString();
        }

        lastNum = numConns;
        numConns = Mathf.Clamp(numConns, MIN_CONNS, MAX_CONNS);

        if (numConns > numHexs && numHexs > 1)
        {
            numConns = numHexs - 1;
        }

        if(lastNum != numConns)
        {
            minConnsInput.text = numConns.ToString();
            maxConnsInput.text = numConns.ToString();
        }
    }

    #endregion

    #region LevelGeneration

    private void CreateFloorLevels()
    {
        for(int i= 0; i<numFloors; i++)
        {
            GameObject floor = null;

            floor = Instantiate(floorPrefab, transform.position - new Vector3(0, i * FLOOR_HEIGHT, 0), transform.rotation, transform);

            floor.transform.name = "Level " + i * (-1);

            SimpleHexLevel floorScript = floor.GetComponent<SimpleHexLevel>();


            //Create the new floor passing the caves to be in the next one
            floorScript.CreateFloor(nextInitCaves, numHexs, numConns, i==numFloors-1, gems.isOn, columns.isOn);

            //add to the floor list
            levelFloorsList.Add(floor);
            levelFloorScriptsList.Add(floorScript);

            //get the new next caves
            nextInitCaves = floorScript.GetNextFloorCaves();

            //amplify next floor
            numHexs++;

            if (floorScript.impossibleFloor)
            {
                RandomSeed();
                GenerateLevel();

                return;
            }
        }
    }

    private void ConnectVerticalHexs()
    {
        SimpleHexLevel levelUp;
        SimpleHexLevel levelDown;

        for (int i = 0; i<levelFloorsList.Count - 1; i++)
        {
            levelUp = levelFloorsList[i].GetComponent<SimpleHexLevel>();
            levelUp.m_graph.ConnectVerticalHexs(levelUp.GetUpFloorCaves(), levelUp.GetNextFloorCaves());
            levelDown = levelFloorsList[i+1].GetComponent<SimpleHexLevel>();
            levelDown.m_graph.ConnectVerticalHexs(levelUp.GetNextFloorCaves(), levelUp.GetUpFloorCaves());
        }
    }

    private void CreateFallingPathInFloor()
    {
        int rnd = UnityEngine.Random.Range(0, levelFloorScriptsList.Count);

        levelFloorScriptsList[rnd].CreateFallingPath();
    }

    private void GenerateRocksInFloor()
    {
        foreach(SimpleHexLevel floor in levelFloorScriptsList)
        {
            floor.GenerateRocks();
        }
    }

    public int GetNumFloors()
    {
        return levelFloorsList.Count;
    }

    #endregion
}