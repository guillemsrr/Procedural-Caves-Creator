using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class LevelFloorsCreator : MonoBehaviour
{
    public static LevelFloorsCreator instance { get; private set; }

    [Header("Level Elements")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject floorDebugPrefab;
    [SerializeField] private List<GameObject> nextInitCaves;
    [SerializeField] private GameObject tunneler;

    [Header("Variables")]
    private int MIN_FLOORS;
    private int MAX_FLOORS;
    private int numFloors;
    private int MIN_NODES;
    private int MAX_NODES;
    private int range;
    private int MIN_NEXT_HEXS;
    private int MAX_NEXT_HEXS;
    private int seed;

    [Header("Debug")]
    [SerializeField] private bool debugMode;
    public int level { get; private set; }
    [SerializeField] private bool infinite;
    [SerializeField] private GameObject debugLight;

    private SimpleHexLevel floorScript;
    private float time;
    private string sceneName;
    private int playerNode = 0;
    public const float FLOOR_HEIGHT = 25f;
    public List<GameObject> levelFloorsList;
    private List<SimpleHexLevel> levelFloorScriptsList;
    private bool lastMapMode;
    private SimpleHexLevel fallingPathFloorScript;

    [Header("Hints")]
    [SerializeField] private GameObject basicControlsHint;
    //[SerializeField] private GameObject mainMissionHint;
    //[SerializeField] private GameObject craftingHint;
    //[SerializeField] private GameObject mapModeHint;

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;



    private void Awake()
    {
        instance = this;

        //Initial Global Variables:
        //seed
        if (infinite)
        {
            seed = UnityEngine.Random.Range(0, System.Int32.MaxValue);
        }

        UnityEngine.Random.InitState(seed);
        Debug.Log(seed);

        CreateRandomSpecs(level);

        time = 0f;
        sceneName = SceneManager.GetActiveScene().name;


        levelFloorsList = new List<GameObject>();
        levelFloorScriptsList = new List<SimpleHexLevel>();

        numFloors = UnityEngine.Random.Range(MIN_FLOORS, MAX_FLOORS);
    }

    void Start()
    {
        //hm...
        //loadingScreen.SetActive(true);
        //Debug.LogError("not active??? " + loadingScreen.activeInHierarchy + " " + loadingScreen.active);

        if (!CreateFloorLevels())
        {
            return;
        }

        ConnectVerticalHexs();
        CreateFallingPathInFloor();
        GenerateRocksInFloor();


        //TUNNEL BLOCKERS
        foreach (SimpleHexLevel floor in levelFloorScriptsList)
        {
            floor.ActivateTunnelBlockers();
        }

        if (debugMode)
        {
            debugLight.SetActive(true);
        }
    }
    
    void Update()
    {
        if (infinite)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                Debug.Log("space");
            }
            else
            {
                time += Time.deltaTime;
                if (time > 2f)
                {

                }
            }
        }
    }

    private bool CreateFloorLevels()
    {
        for(int i= 0; i<numFloors; i++)
        {
            GameObject floor = null;
            if (!debugMode)
                floor = Instantiate(floorPrefab, transform.position - new Vector3(0, i * FLOOR_HEIGHT, 0), transform.rotation, transform);
            else
                floor = Instantiate(floorDebugPrefab, transform.position - new Vector3(0, i * FLOOR_HEIGHT, 0), transform.rotation, transform);

            floor.transform.name = "Level " + i * (-1);

            floorScript = floor.GetComponent<SimpleHexLevel>();

            
            //Create the new floor passing the caves to be in the next one
            //if (i != numFloors - 1)
            //{
            //    floorScript.CreateFloor(nextInitCaves, MIN_NODES, MAX_NODES, range, MIN_NEXT_HEXS, MAX_NEXT_HEXS, level:levelNum);
            //}
            //else
            //{
            //    if(LevelObjectives.instance.mainObjective.objType == MainObjectiveTypes.OBSTACLE)
            //    {
            //        floorScript.CreateFloor(nextInitCaves, MIN_NODES, MAX_NODES, range, MIN_NEXT_HEXS, MAX_NEXT_HEXS, true, true, levelNum);
            //    }
            //    else
            //    {
            //        floorScript.CreateFloor(nextInitCaves, MIN_NODES, MAX_NODES, range, MIN_NEXT_HEXS, MAX_NEXT_HEXS, true, false, levelNum);
            //    }
            //}   

            //add to the floor list
            levelFloorsList.Add(floor);
            levelFloorScriptsList.Add(floorScript);

            //get the new next caves
            nextInitCaves = floorScript.GetNextFloorCaves();

            //amplify next floor
            MIN_NODES++;
            MAX_NODES++;

            Debug.Log("floor " + floor.transform.name + " instantiated");
            if (floorScript.impossibleFloor)
            {
                return false;
            }
        }
        return true;
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
        //DEBUG---
        //levelFloorScriptsList[0].CreateFallingPath();
        //fallingPathFloorScript = levelFloorScriptsList[0];

        if (level > 1)
        {
            if (UnityEngine.Random.Range(0, 100) <= level * 13)
            {
                int rnd = UnityEngine.Random.Range(0, levelFloorScriptsList.Count);

                levelFloorScriptsList[rnd].CreateFallingPath();
                fallingPathFloorScript = levelFloorScriptsList[rnd];
            }
        }
    }

    private void GenerateRocksInFloor()
    {
        foreach(SimpleHexLevel floor in levelFloorScriptsList)
        {
            floor.GenerateRocks();
        }
    }

    private void CreateRandomSpecs(int _level)
    {
        switch (_level)
        {
            //de moment hi ha 10 nivells
            case 1:
                MIN_FLOORS = 2;
                MAX_FLOORS = 2;

                MIN_NODES = 2;
                MAX_NODES = 3;

                MIN_NEXT_HEXS = 1;
                MAX_NEXT_HEXS = 1;

                break;
            case 2:
                MIN_FLOORS = 2;
                MAX_FLOORS = 2;

                MIN_NODES = 3;
                MAX_NODES = 4;

                MIN_NEXT_HEXS = 1;
                MAX_NEXT_HEXS = 1;

                break;
            case 3:
                MIN_FLOORS = 2;
                MAX_FLOORS = 3;

                MIN_NODES = 3;
                MAX_NODES = 5;

                MIN_NEXT_HEXS = 1;
                MAX_NEXT_HEXS = 2;

                break;
            case 4:
                MIN_FLOORS = 2;
                MAX_FLOORS = 3;

                MIN_NODES = 4;
                MAX_NODES = 7;

                MIN_NEXT_HEXS = 2;
                MAX_NEXT_HEXS = 2;

                break;
            case 5:
                MIN_FLOORS = 2;
                MAX_FLOORS = 3;

                MIN_NODES = 4;
                MAX_NODES = 8;

                MIN_NEXT_HEXS = 2;
                MAX_NEXT_HEXS = 2;

                break;
            case 6:
                MIN_FLOORS = 3;
                MAX_FLOORS = 4;

                MIN_NODES = 8;
                MAX_NODES = 10;

                MIN_NEXT_HEXS = 2;
                MAX_NEXT_HEXS = 3;

                break;
            case 7:
                MIN_FLOORS = 3;
                MAX_FLOORS = 4;

                MIN_NODES = 8;
                MAX_NODES = 14;

                MIN_NEXT_HEXS = 2;
                MAX_NEXT_HEXS = 3;

                break;
            case 8:
                MIN_FLOORS = 3;
                MAX_FLOORS = 4;

                MIN_NODES = 8;
                MAX_NODES = 16;

                MIN_NEXT_HEXS = 2;
                MAX_NEXT_HEXS = 3;

                break;
            case 9:
                MIN_FLOORS = 3;
                MAX_FLOORS = 4;

                MIN_NODES = 8;
                MAX_NODES = 18;

                MIN_NEXT_HEXS = 2;
                MAX_NEXT_HEXS = 3;

                break;
            case 10:
                MIN_FLOORS = 4;
                MAX_FLOORS = 4;

                MIN_NODES = 15;
                MAX_NODES = 20;

                MIN_NEXT_HEXS = 3;
                MAX_NEXT_HEXS = 3;

                break;
            default:
                MIN_FLOORS = 4;
                MAX_FLOORS = 4;

                MIN_NODES = 30;
                MAX_NODES = 40;

                MIN_NEXT_HEXS = 5;
                MAX_NEXT_HEXS = 10;

                break;
        }
    }


    public int GetNumFloors()
    {
        return levelFloorsList.Count;
    }
}
