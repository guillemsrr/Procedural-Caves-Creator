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
    [SerializeField] private List<GameObject> nextInitCaves;

    [Header("Variables")]
    [SerializeField] private int MIN_FLOORS;
    [SerializeField] private int MAX_FLOORS;
    [SerializeField] private int numFloors;
    [SerializeField] private int MIN_NODES;
    [SerializeField] private int MAX_NODES;
    [SerializeField] private int MIN_NEXT_HEXS;
    [SerializeField] private int MAX_NEXT_HEXS;
    [SerializeField] private int seed;

    public int level { get; private set; }
    [SerializeField] private bool infinite;
    private float time;
    private string sceneName;
    public const float FLOOR_HEIGHT = 25f;
    public List<GameObject> levelFloorsList;
    private List<SimpleHexLevel> levelFloorScriptsList;
    private SimpleHexLevel fallingPathFloorScript;


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

        time = 0f;
        sceneName = SceneManager.GetActiveScene().name;


        levelFloorsList = new List<GameObject>();
        levelFloorScriptsList = new List<SimpleHexLevel>();

        numFloors = UnityEngine.Random.Range(MIN_FLOORS, MAX_FLOORS);
    }

    void Start()
    {
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

            floor = Instantiate(floorPrefab, transform.position - new Vector3(0, i * FLOOR_HEIGHT, 0), transform.rotation, transform);

            floor.transform.name = "Level " + i * (-1);

            SimpleHexLevel floorScript = floor.GetComponent<SimpleHexLevel>();


            //Create the new floor passing the caves to be in the next one
            floorScript.CreateFloor(nextInitCaves, MIN_NODES, MAX_NODES, MIN_NEXT_HEXS, MAX_NEXT_HEXS, i==numFloors-1, level);

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
        int rnd = UnityEngine.Random.Range(0, levelFloorScriptsList.Count);

        levelFloorScriptsList[rnd].CreateFallingPath();
        fallingPathFloorScript = levelFloorScriptsList[rnd];
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
}
