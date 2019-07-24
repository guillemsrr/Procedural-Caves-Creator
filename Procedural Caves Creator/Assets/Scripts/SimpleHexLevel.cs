using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class SimpleHexLevel : MonoBehaviour
{
    #region Prefabs
    [Header ("Cave Prefabs") ]
    [SerializeField] private GameObject HexPrefab;
    [SerializeField] private GameObject HexHoledDownPrefab;
    [SerializeField] private GameObject HexHoledUpPrefab;
    [SerializeField] private GameObject emptyCavePrefab;

    [Header("Wall Prefabs")]
    [SerializeField] private GameObject wall_Left_A_T_Prefab;
    [SerializeField] private GameObject wall_Right_A_T_Prefab;
    [SerializeField] private GameObject wall_Left_B_Prefab;
    [SerializeField] private GameObject wall_Left_B_T_Prefab;
    [SerializeField] private GameObject wall_Right_B_Prefab;
    [SerializeField] private GameObject wall_Right_B_T_Prefab;

    [Header("__Tunnel Prefabs__")]
    [Header("Vertical")]
    [SerializeField] private GameObject tunnel_Cap_A_Prefab;
    [SerializeField] private GameObject tunnel_Cap_B_Prefab;
    [SerializeField] private GameObject tunnel_mid_forward;
    [SerializeField] private GameObject tunnel_mid_vertical;
    [Header("Diagonal")]
    [SerializeField] private GameObject tunnelDiag_Cap_L_Prefab;
    [SerializeField] private GameObject tunnelDiag_Cap_R_Prefab;
    [SerializeField] private GameObject tunnelDiag_Connector_LR_Prefab;
    [SerializeField] private GameObject tunnelDiag_Connector_RL_Prefab;
    [SerializeField] private GameObject tunnelDiag_Left_Prefab;
    [SerializeField] private GameObject tunnelDiag_Right_Prefab;
    [Header("Rappel")]
    [SerializeField] private GameObject rappelDownPrefab;
    [SerializeField] private GameObject rappelUpPrefab;

    [Header("ExtraCave Prefabs")]
    [SerializeField] private GameObject fallingPathHexPrefab;
    [SerializeField] private GameObject hugeWallPrefab;
    
    [Header("Game Elements")]
    [SerializeField] private GameObject m_rock;
    [Header("Gems")]
    [SerializeField] private GameObject[] gemPrefabs;

    [Header("Rocks")]
    [SerializeField] private GameObject[] rocks_walkable;

    #endregion

    #region Variables

    private int maxRange;
    private List<GameObject> cavesList;
    private int numNodes;
    private int nodeNum;
    private int hexConnectionNum;
    private int numNextHexs;
    public Graph m_graph { get; private set; }
    private bool lastFloor;
    private List<GameObject> nextFloorHexs;
    private List<GameObject> upConnectionHexs;
    private bool upDownPlatformerInstantiated;
    private bool spawnedLastCave;
    private GameObject lastCave;
    private List<GameObject> pathHexList;
    private List<Vector3> propsPositions;

    //rocks:
    private float minRockDist = 1f;
    private float maxRockDist = 4f;
    private int rejectionLimit = 30;

    public bool impossibleFloor = false;
    [SerializeField] private GameObject help;

    #endregion

    #region Floor

    public void CreateFloor(List<GameObject> init, int _numNodes, int _numConns, bool isLast, bool spawnGems, bool spawnColumns)
    {
        numNodes = _numNodes;
        numNextHexs = _numConns;
        SetRange();

        nodeNum = -1;
        hexConnectionNum = 0;
        lastFloor = isLast;

        upDownPlatformerInstantiated = false;
        cavesList = new List<GameObject>();
        nextFloorHexs = new List<GameObject>();
        upConnectionHexs = new List<GameObject>();
        pathHexList = new List<GameObject>();

        foreach (GameObject hex in init)
        {
            GameObject cave = Instantiate(emptyCavePrefab, hex.transform.position, transform.rotation, transform);
            cave.name = "Cave " + cavesList.Count;
            hex.transform.SetParent(cave.transform);
            if (hex.name.Contains("Trap"))
                hex.name = "Hex " + nodeNum + " FloorConnection Trap Down " + hexConnectionNum;
            else if (!hex.name.Contains("START"))
                hex.name = "Hex " + nodeNum + " FloorConnection UpDown " + hexConnectionNum;
            cavesList.Add(cave);
            nodeNum++;
            hexConnectionNum++;
        }
        hexConnectionNum = 0;

        CreateCaves();
        CutWalls();
        CreateVerticalTunnels();
        CreateGraph();
        if (impossibleFloor)
        {
            return;
        }
        CreateTunnels();
        FillCaves(spawnGems, spawnColumns);

        //Debug.Log("floor " + transform.name + " CreateFloor done");
    }

    private void CreateCaves()
    {
        for (int i = nodeNum; i < numNodes; i++)
        {
            if (lastFloor && !spawnedLastCave && transform.parent.childCount > 1)
            {
                CreateLastCave();//provisional, necessito fer-ho des de Graph per poder guardar la dada de lastCave
            }

            //get new position
            Vector3 pos = RandomCavePosition();
            int cut = 0;
            while (!isAvailable(pos) && cut < 10)//check if it's already used
            {
                cut++;
                pos = RandomCavePosition();
            }

            //check if that position is near another cave-hex and attach it
            if (!CheckNearHex(pos, i))
            {
                //we need to create a new cave with the new hex
                GameObject cave = Instantiate(emptyCavePrefab, pos, transform.rotation, transform);
                cave.name = "Cave " + cavesList.Count;
                cavesList.Add(cave);

                if (!LastChanceNextCave(i, pos, cave))
                {
                    GameObject hex = Instantiate(HexPrefab, pos, HexPrefab.transform.rotation, cave.transform);
                    hex.name = "Hex " + i;
                }
            }
        }

        //Recheck nexes connection with other caves, due to the previous instantiation order
        RecheckNearHex();

        //Rename all caves, because wemay have delete some of them empty
        RenameCaves();
    }

    private Vector3 RandomCavePosition()
    {
        //____X position____
        float x = Graph.RDist + Graph.RDist / 2f;

        int rangeX = Random.Range(0, maxRange + 1);

        //positive or negative?
        if (Random.Range(0f, 1f) < .5f)
        {
            x *= -1;
        }
        x *= rangeX;

        //____Z position____
        float z = Graph.rDist;
        int rangeZ = Random.Range(0, maxRange + 1);

        if (rangeX % 2 == 0 || x == 0)//parell, even
        {
            z = Graph.rDist * 2;
        }
        else if (rangeZ % 2 == 0)//senar, odd
        {
            rangeZ++;
        }

        //positive or negative?
        if (Random.Range(0f, 1f) < .5f)
        {
            z *= -1;
        }

        z *= rangeZ;

        return transform.position + new Vector3(x, 0, z);
    }

    private bool isAvailable(Vector3 pos)
    {
        if (pos.x == 0 && pos.z == 0 && !lastFloor)
            return false;
        foreach (GameObject cave in cavesList)
        {
            foreach(Transform hex in cave.transform)
            {
                if (hex.position == pos)
                    return false;
            }
        }
        return true;
    }

    private bool CheckNearHex(Vector3 pos, int i)
    {
        foreach (GameObject cave in cavesList)
        {
            foreach (Transform hex in cave.transform)
            {
                if (Vector3.Distance(hex.position, pos) <= Graph.rDist * 2)
                {
                    //Next Cave, (it will probably be on the most lateral caves)
                    if(!LastChanceNextCave(i, pos, cave))
                    {
                        GameObject h = Instantiate(HexPrefab, pos, HexPrefab.transform.rotation, cave.transform);
                        h.name = "Hex " + i;
                    }
                    return true;
                }  
            }
        }
        return false;
    }

    private void CreateLastCave()
    {
        Vector3 pos = new Vector3(0, transform.position.y, 0);
        GameObject cave = Instantiate(emptyCavePrefab, pos, transform.rotation, transform);
        cave.name = " Last Cave ";
        cavesList.Add(cave);
        lastCave = Instantiate(HexPrefab, pos, HexPrefab.transform.rotation, cave.transform);
        lastCave.name = "Hex GOAL";
        spawnedLastCave = true;
    }

    private bool LastChanceNextCave(int i, Vector3 pos, GameObject cave)
    {
        if (numNextHexs >= numNodes - i && !lastFloor)
        {
            if (!upDownPlatformerInstantiated) //instanciate UpDown
            {
                GameObject h = Instantiate(HexHoledDownPrefab, pos, HexHoledDownPrefab.transform.rotation, cave.transform);
                h.name = "Hex " + i + " FloorConnection UpDown " + hexConnectionNum;
                upConnectionHexs.Add(h);
                hexConnectionNum++;
                GameObject nextHex = Instantiate(HexHoledUpPrefab, pos - new Vector3(0, LevelFloorsCreator.FLOOR_HEIGHT, 0), HexHoledUpPrefab.transform.rotation, transform.parent);
                nextHex.name = "UpDown";
                nextFloorHexs.Add(nextHex);
                numNextHexs--;
                upDownPlatformerInstantiated = true;
            }
            else //Trap
            {
                GameObject h = Instantiate(HexHoledDownPrefab, pos, HexHoledDownPrefab.transform.rotation, cave.transform);
                h.name = "Hex " + i + " FloorConnection Trap Up " + hexConnectionNum;
                upConnectionHexs.Add(h);
                hexConnectionNum++;
                GameObject nextHex = Instantiate(HexHoledUpPrefab, pos - new Vector3(0, LevelFloorsCreator.FLOOR_HEIGHT, 0), HexHoledUpPrefab.transform.rotation, transform.parent);
                nextHex.name = "Trap";
                nextFloorHexs.Add(nextHex);
                numNextHexs--;
            }
            return true;
        }
        return false;
    }

    private void RecheckNearHex()
    {
        int lastCave = cavesList.Count - 1;
        for (int i = lastCave; i > 0; i--)
        {
            CheckAllOtherHexes(cavesList[i]);
        }
    }

    private void CheckAllOtherHexes(GameObject cave)
    {
        foreach (Transform hex in cave.transform)
        {
            foreach (GameObject cave2 in cavesList)
            {
                if (cave != cave2)
                {
                    foreach (Transform hex2 in cave2.transform)
                    {
                        if (Vector3.Distance(hex.position, hex2.position) <= Graph.rDist * 2 + 1f)//un magic number com una catedral, sinó no funciona per una petita acumulació d'errors molt estranya...
                        {
                            Transform[] caveChilds = new Transform[cave.transform.childCount];

                            int it = 0;
                            foreach(Transform h in cave.transform)
                            {
                                caveChilds[it] = h;
                                it++;
                            }

                            foreach(Transform h in caveChilds)
                            {
                                h.SetParent(cave2.transform);
                            }

                            if (cave.transform.childCount == 0)
                            {
                                Destroy(cave.gameObject);
                                cavesList.Remove(cave);
                            }
                            return;
                        }
                    }
                }
            }
        }
    }

    private void RenameCaves()
    {
        for (int i = 0; i < cavesList.Count; i++)
        {
            cavesList[i].transform.name = "Cave " + i;
        }
    }

    private void CutWalls()
    {
        List<GameObject> facesToDelete = new List<GameObject>();
        foreach (GameObject cave in cavesList)
        {
            foreach (Transform hex in cave.transform)
            {
                foreach (Transform face in hex)
                {
                    if(face.name.Contains("face"))
                        GetOverlappedFaces(cave, hex, face, facesToDelete);
                }
            }
        }

        while (facesToDelete.Count != 0)
        {
            if (facesToDelete[0] != null)
            {
                ChangeNeighborWalls(facesToDelete[0].transform);
                DestroyImmediate(facesToDelete[0]);
            }
            //DebugTools.DebugDestroyObject(gameObject.name, facesToDelete[0]);
            facesToDelete.Remove(facesToDelete[0]);
        }
    }

    private void GetOverlappedFaces(GameObject cave, Transform hex, Transform face, List<GameObject> deleteFace)
    {
        foreach (Transform hex2 in cave.transform)
        {
            if (hex2 != hex)
            {
                foreach (Transform face2 in hex2)
                {
                    if (face2.name.Contains("face"))
                    {
                        if (Vector3.Distance(face.position, face2.position) <= 1f)
                        {
                            deleteFace.Add(face.gameObject);
                            deleteFace.Add(face2.gameObject);

                            return;
                        }
                    }
                }
            }
        }
    }

    private void ChangeNeighborWalls(Transform deletedFace)
    {
        foreach (Transform face in deletedFace.parent)
        {
            if (face.name.Contains("face"))//sense això hi ha un bug, sembla que posa els rappels com a faces, o alguna cosa dels tunnels verticals...
            {
                if (face != deletedFace && Vector3.Distance(face.position, deletedFace.position) <= Graph.RDist + 1f)
                {
                    SwapWalls(face, deletedFace);
                }
            }
        }
    }

    private void SwapWalls(Transform face, Transform deletedFace)
    {
        if(isRightSideWall(face, deletedFace))
        {
            Transform oldWall = face.GetChild(1);
            if(oldWall.name.Contains("Left"))
                oldWall = face.GetChild(0);

            Instantiate(wall_Right_B_Prefab, oldWall.position, oldWall.rotation, face.transform);
            DestroyImmediate(oldWall.gameObject);
            //DebugTools.DebugDestroyObject(gameObject.name, oldWall.gameObject);
        }
        else
        {
            Transform oldWall = face.GetChild(0);
            if (oldWall.name.Contains("Right"))
                oldWall = face.GetChild(1);
            Instantiate(wall_Left_B_Prefab, oldWall.position, oldWall.rotation, face.transform);
            DestroyImmediate(oldWall.gameObject);
            //DebugTools.DebugDestroyObject(gameObject.name, oldWall.gameObject);
        }
    }

    private void TunnelFace(Transform face)
    {
        Transform oldWall1 = face.GetChild(0);
        Transform oldWall2 = face.GetChild(1);

        SwapTunnelWalls(oldWall1);
        SwapTunnelWalls(oldWall2);
    }

    private void SwapTunnelWalls(Transform oldWall)
    {
        if (oldWall.name.Contains("Left_A"))
        {
            Instantiate(wall_Left_A_T_Prefab, oldWall.position, oldWall.rotation, oldWall.parent);
        }
        else if (oldWall.name.Contains("Left_B"))
        {
            Instantiate(wall_Left_B_T_Prefab, oldWall.position, oldWall.rotation, oldWall.parent);
        }
        else if (oldWall.name.Contains("Right_A"))
        {
            Instantiate(wall_Right_A_T_Prefab, oldWall.position, oldWall.rotation, oldWall.parent);
        }
        else if (oldWall.name.Contains("Right_B"))
        {
            Instantiate(wall_Right_B_T_Prefab, oldWall.position, oldWall.rotation, oldWall.parent);
        }

        DestroyImmediate(oldWall.gameObject);
    }

    private bool isRightSideWall(Transform face, Transform deletedFace)
    {
        Vector3 dirVec = face.position - face.parent.position;
        Vector3 sideVec = deletedFace.position - deletedFace.parent.position;
        float angle = Vector3.SignedAngle(dirVec, sideVec, Vector3.up);

        return angle >= 0f;
    }

    private void CreateGraph()
    {
        //first create the graph and its nodes
        //Random.InitState(Random.Range(0, System.Int32.MaxValue));

        m_graph = gameObject.AddComponent<Graph>();
        m_graph.CreateGraph(cavesList, lastFloor, help);

        //then the edges, and get the graph connected
        m_graph.GetPossibleConnections();
        m_graph.CreateEdges();

        int x = 0;
        while (!m_graph.connectedNodes && x < 2)//restart again in case we don't get a connected Graph, at least from floor connection to floor connection
        {
            x++;
            //change level seed?¿?¿
            //LevelFullSpecification.Instance.SetSeed(Random.Range(0, System.Int32.MaxValue));

            Destroy(gameObject.GetComponent<Graph>());
            m_graph = gameObject.AddComponent<Graph>();
            m_graph.CreateGraph(cavesList, lastFloor, help);

            //then the edges, and get the graph connected
            m_graph.GetPossibleConnections();
            m_graph.CreateEdges();
        }

        if(x >= 2)
        {
            Debug.Log("HAD TO REMAKE LEVEL");
            impossibleFloor = true;
            return;
        }

        //in the next function we actually create the tunnels
    }

    private void CreateTunnels()
    {
        foreach (Graph.Edge edge in m_graph.edgeList)
        {
            GameObject tunnel = Instantiate(emptyCavePrefab, edge.face1.position, edge.face1.rotation, transform);//i put it in transform so that is easier to activate and deactivate
            tunnel.name = "Tunnel from Cave " + edge.node1.number + " - " + edge.node2.number + " in Hex " + edge.face1.parent.name;

            //here we add the info to the node graph
            edge.node1.tunnelList.Add(tunnel);
            edge.node2.tunnelList.Add(tunnel);

            edge.node1.edgeList.Add(edge);
            edge.node2.edgeList.Add(edge);

            int numPart = 0;
            edge.mesh = tunnel;

            if(edge.face1.childCount == 0 || edge.face2.childCount == 0)
            {
                //Debug.Log("edge face without childs: " + edge.face1.name);
                //Debug.Log("hex parent: " + edge.face1.parent.name);
                return;
            }

            Vector3 startPos = edge.face1.GetChild(0).position;
            bool fullTunnel = false;

            if (edge.isForward)
            {
                //place both caps:
                GameObject tunnelPart = Instantiate(tunnel_Cap_A_Prefab, startPos, edge.face1.GetChild(0).rotation, tunnel.transform);
                tunnelPart.name = "Tunnel Cap A";


                tunnelPart = Instantiate(tunnel_Cap_B_Prefab, edge.face2.transform.position, edge.face2.GetChild(0).rotation * Quaternion.Euler(0, 0f, 180), tunnel.transform);
                //little correction:
                tunnelPart.transform.position -= tunnelPart.transform.right * 0.1f;
                tunnelPart.name = "Tunnel Cap B";

                //startPos += tunnelPart.transform.right * Graph.rDist * 2;
                float tunnelDistance = Graph.rDist * 2;
                float facesDistance = Vector3.Distance(edge.face1.transform.position, edge.face2.transform.position) - 0.1f;

                if (tunnelDistance >= facesDistance)
                    fullTunnel = true;

                while (!fullTunnel)
                {
                    tunnelPart = Instantiate(tunnel_mid_forward, startPos, edge.face1.GetChild(0).rotation, tunnel.transform);
                    tunnelPart.name = "Tunnel Part " + numPart;
                    numPart++;

                    startPos += tunnelPart.transform.right * Graph.rDist*2;
                    tunnelDistance += Graph.rDist * 2;

                    if (tunnelDistance >= facesDistance)
                    {
                        fullTunnel = true;
                    }
                }
            }
            else //diagonal
            {
                //detect the angle between both faces
                //float angle = Vector3.SignedAngle(Vector3.Normalize(edge.face2.GetChild(0).position), edge.face1.GetChild(0).parent.parent.forward, tunnel.transform.forward);
                float angle = Vector3.SignedAngle(edge.face1.transform.position - edge.face2.transform.position, edge.face1.transform.forward, Vector3.up);

                GameObject capA = null;
                GameObject capB = null;

                bool isRight = false;

                if (angle > 0)//right
                {
                    capA = tunnelDiag_Cap_R_Prefab;
                    capB = tunnelDiag_Cap_L_Prefab;

                    isRight = true;
                }
                else//left
                {
                    capA = tunnelDiag_Cap_L_Prefab;
                    capB = tunnelDiag_Cap_R_Prefab;
                }

                //place both caps:
                GameObject tunnelPart = Instantiate(capA, startPos, edge.face1.GetChild(0).rotation, tunnel.transform);
                tunnelPart.transform.Rotate(new Vector3(90, 0,0));
                tunnelPart.name = "Tunnel Cap A";

                tunnelPart = Instantiate(capB, edge.face2.transform.position, edge.face2.GetChild(0).rotation, tunnel.transform);
                tunnelPart.transform.Rotate(new Vector3(90, 0,0));
                //little correction:
                tunnelPart.transform.position += tunnelPart.transform.right * 0.01f;
                tunnelPart.name = "Tunnel Cap B";

                int saviour = 0;
                //middle parts:
                Quaternion rot = edge.face1.GetChild(0).rotation;
                while (!fullTunnel && saviour < 3)
                {
                    saviour++;
                    if (isRight)
                    {
                        tunnelPart = Instantiate(tunnelDiag_Right_Prefab, startPos, rot, tunnel.transform);
                    }
                    else
                    {
                        tunnelPart = Instantiate(tunnelDiag_Left_Prefab, startPos, rot, tunnel.transform);
                    }

                    tunnelPart.transform.Rotate(new Vector3(90, 0, 0));
                    //tunnelPart.name = "Tunnel Part " + numPart;
                    numPart++;

                    Transform sphere = tunnelPart.transform.GetChild(0);
                    startPos = sphere.position;
                    startPos += sphere.right*2.3f;

                    if (Vector3.Distance(startPos, edge.face2.position) < 1f)
                    {
                        fullTunnel = true;
                    }
                    else
                    {
                        if (isRight)
                        {
                            tunnelPart = Instantiate(tunnelDiag_Connector_RL_Prefab, startPos, sphere.rotation, tunnel.transform);
                        }
                        else
                        {
                            tunnelPart = Instantiate(tunnelDiag_Connector_LR_Prefab, startPos, sphere.rotation, tunnel.transform);
                        }

                        tunnelPart.transform.Rotate(new Vector3(90, 0, 0));
                        //startPos += sphere.right * 2.3f;
                        rot = sphere.rotation;
                        Destroy(sphere.gameObject);
                    }

                    isRight = !isRight;
                }
            }

            edge.node1.tunnelHexDict.Add(tunnel, edge.face1.parent.gameObject);
            edge.node2.tunnelHexDict.Add(tunnel, edge.face2.parent.gameObject);

            TunnelFace(edge.face1);
            TunnelFace(edge.face2);
        }
    }

    private void CreateVerticalTunnels()
    {
        int i = 0;
        foreach(GameObject hex in nextFloorHexs)
        {
            if (hex.name.Contains("Trap"))
            {
                GameObject tunnelPart = Instantiate(tunnel_mid_vertical, hex.transform.position - new Vector3 (0, -25 - Graph.rDist + 0.1f, 0), Quaternion.Euler(0,0,-90), hex.transform);
                tunnelPart.transform.position -= new Vector3(0, 0, Graph.rDist/4);
                tunnelPart.name = "Vertical Tunnel Trap";
            }
            else if(hex.name.Contains("UpDown"))
            {
                GameObject tunnelPart = Instantiate(tunnel_mid_vertical, hex.transform.position - new Vector3(0, -25 - Graph.rDist + 0.1f, 0), Quaternion.Euler(0, 0, -90), hex.transform);
                tunnelPart.transform.position -= new Vector3(0, 0, Graph.rDist / 4);
                tunnelPart.name = "UpDown Tunnel";

                //Create Rappels:
                GameObject rappel = Instantiate(rappelDownPrefab, hex.transform.position - new Vector3(0, -25, 0), Quaternion.identity, upConnectionHexs[i].transform);
                rappel = Instantiate(rappelUpPrefab, hex.transform.position, Quaternion.identity, hex.transform);
            }

            i++;
        }
    }

    public List<GameObject> GetNextFloorCaves()
    {
        return nextFloorHexs;
    }

    public List<GameObject> GetUpFloorCaves()
    {
        return upConnectionHexs;
    }

    private void CorrectNodes()
    {
        int allNodes = numNodes + numNextHexs;
        if (lastFloor)
            allNodes++;

        int maxNodes = maxRange * maxRange;

        if(allNodes > maxNodes)
        {
            if(allNodes > maxNodes * 1.5)
            {
                maxRange++;
                Debug.Log("maxRange in " + gameObject.name + " corrected to " + maxRange);
            }
            else
            {
                int rest = allNodes - maxNodes;
                numNodes -= rest;
                Debug.Log("numNodes in " + gameObject.name + " corrected to " + numNodes);
            }
        }
    }

    private void SetRange()
    {
        int allNodes = numNodes + numNextHexs;
        if (lastFloor)
            allNodes++;

        maxRange = (int)Mathf.Sqrt(allNodes);
        maxRange--;

        int maxNodes = maxRange * maxRange;
        if (allNodes > maxNodes)
        {
            //Debug.Log("allNodes: " + allNodes);
            //Debug.Log("maxNodes: " + maxNodes);
            //Debug.Log("rest: " + (allNodes - maxNodes));

            if (allNodes > maxNodes * 4)
            {
                maxRange++;
                maxRange++;
                //Debug.Log("double maxRange changed");
            }
            else if(allNodes > maxNodes * 1.5)
            {
                maxRange++;
                //Debug.Log("maxRange changed");
            }
            else
            {
                numNodes -= allNodes - maxNodes;
                //Debug.Log("numNodes changed");
            }
        }
    }
    #endregion

    #region GameElements

    private void FillCaves(bool spawnGems, bool spawnCols)
    {
        propsPositions = new List<Vector3>();

        foreach (Graph.Node n in m_graph.nodeList)
        {
            int counter = 0; // to make sure some element is limited inside the cave

            switch (n.m_type)
            {
                case Graph.CaveType.START:
                    foreach (Transform hex in n.hexsList)
                    {
                        propsPositions.Clear();

                        if (hex.name.Contains("START"))
                        {
                            //only the tunneler
                        }
                        else
                        {
                            hex.name += " start neighbor";

                            if (spawnGems)
                                CreateProps(gemPrefabs, n, hex, 15, 3, propsPositions);
                            if(spawnCols)
                                CreateProp(m_rock, n , hex, 25, 10, propsPositions, 0f, 4);
                        }
                    }
                    break;
                case Graph.CaveType.UPDOWN_CONNECTION:
                    foreach (Transform hex in n.hexsList)
                    {
                        if (hex.name.Contains("Connection"))
                        {
                            if (spawnGems)
                                CreateProps(gemPrefabs, n , hex, 15, 2, propsPositions, minRad:5);
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 10, propsPositions, minRad: 2);
                        }
                        else
                        {
                            hex.name += " connection neighbor";
                            if (spawnGems)
                                CreateProps(gemPrefabs, n , hex, 25, 3, propsPositions);
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 10, propsPositions, minRad: 2);
                        }
                    }
                    break;
                case Graph.CaveType.TRAP:
                    foreach (Transform hex in n.hexsList)
                    {
                        if (hex.name.Contains("Connection"))
                        {
                            hex.name += " trap";
                            if (spawnGems)
                                CreateProps(gemPrefabs, n , hex, 15, 3, propsPositions, minRad: 5);
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 3, propsPositions, minRad: 2);
                        }
                        else
                        {
                            hex.name += " trap neighbor";

                            if (spawnGems)
                                CreateProps(gemPrefabs, n , hex, 25, 5, propsPositions);
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 10, propsPositions, minRad:2);
                        }
                    }
                    break;
                case Graph.CaveType.ROCKY:
                    foreach (Transform hex in n.hexsList)
                    {
                        hex.name += " rocky";
                        if (spawnGems)
                            CreateProps(gemPrefabs, n , hex, 10, 2, propsPositions);
                        if (spawnCols)
                            CreateProp(m_rock, n , hex, 70, 6, propsPositions);
                    }
                    break;
                case Graph.CaveType.NORMAL:
                    foreach (Transform hex in n.hexsList)
                    {
                        hex.name += " normal";
                        if (spawnGems)
                            CreateProps(gemPrefabs, n , hex, 25, 2, propsPositions);
                        if (spawnCols)
                            CreateProp(m_rock, n , hex, 25, 5, propsPositions, minRad:2);
                    }
                    break;
                case Graph.CaveType.MINE:
                    foreach (Transform hex in n.hexsList)
                    {
                        hex.name += " ygz mine";
                        if (spawnCols)
                            CreateProp(m_rock, n , hex, 25, 2, propsPositions, minRad:2);
                        if (spawnGems)
                            CreateProps(gemPrefabs, n , hex, 75, 5, propsPositions);
                    }
                    break;
                case Graph.CaveType.COMBINED://there are many connections in the same cave
                    foreach (Transform hex in n.hexsList)
                    {
                        if (hex.name.Contains("UpDown"))
                        {
                            if (spawnGems)
                                CreateProps(gemPrefabs, n , hex, 15, 3, propsPositions, minRad:5);
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 5, propsPositions, minRad:4);

                        }
                        else if (hex.name.Contains("Trap Up"))
                        {
                            if (spawnGems)
                                CreateProps(gemPrefabs, n , hex, 15, 3, propsPositions, 0.2f, 5);
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 3, propsPositions, 0f, 4);
                        }
                        else if (hex.name.Contains("Trap Down"))
                        {
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 2, propsPositions, 0f, 2);
                        }
                        else if (hex.name.Contains("START"))
                        {

                        }
                        else
                        {
                            hex.name += " normal combined neighbor";
                            if (spawnGems)
                                CreateProps(gemPrefabs, n , hex, 25, 2, propsPositions);
                            if (spawnCols)
                                CreateProp(m_rock, n , hex, 25, 5, propsPositions, minRad: 2);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void CreateProp(GameObject prop, Graph.Node node, Transform hex, int percentage, int max, List<Vector3> propsPositions, float minDist = 0f, float minRad = 0f, float maxRad = 8f, float height = 0f, bool faceCenter = false)
    {
        if (height == 0f)
            height = GetPropHeight(prop);

        if (minDist == 0f)
            minDist = GetPropMinDist(prop);

        for (int i = 0; i < max; i++)
        {
            if (Random.Range(0, 100) < percentage)
            {
                Vector3 pos = RandomObjectPositionInHex(minRad, maxRad, height);

                int rescuer = 0;
                while (TooCloseToOtherProp(pos, propsPositions, minDist)||TooCloseFromEntrance(pos, hex))
                {
                    if (rescuer > 100 || minDist < 0.2f)
                    {
                        if (prop.name.Contains("Gem"))//we'd need to destroy the objects the gem is colliding with:
                        {
                            List<GameObject> toDestroyObjects = new List<GameObject>();
                            foreach(Transform child in hex)
                            {
                                if(Vector3.Distance(child.position, pos + hex.position) < minDist)
                                {
                                    toDestroyObjects.Add(child.gameObject);
                                }
                            }

                            while (toDestroyObjects.Count != 0)
                            {
                                Destroy(toDestroyObjects[0]);
                                toDestroyObjects.RemoveAt(0);
                            }

                            break;
                        }
                        else
                        {
                            Debug.Log("Rescued from infinite Loop on Creating " + prop.name);
                            return;
                        }
                    }
                    minDist -= 0.05f;
                    pos = RandomObjectPositionInHex(minRad, maxRad, height);
                    rescuer++;
                }

                propsPositions.Add(pos);
                Quaternion rot = Quaternion.identity;
                if (!faceCenter)
                {
                    rot = Quaternion.Euler(0, RandomPoint(180), 0);
                }
                else
                {
                    rot = Quaternion.LookRotation(-pos);
                }
               
                GameObject p = Instantiate(prop, pos + hex.position, rot, hex.transform);
            }
        }
    }

    private void CreateProps(GameObject[] props, Graph.Node node, Transform hex, int percentage, int max, List<Vector3> propsPositions, float minDist = 0f, float minRad = 0f, float maxRad = 8f, float height = 0f, bool faceCenter = false)
    {
        GameObject prop = props[Random.Range(0, props.Length)];
        if (height == 0f)
            height = GetPropHeight(prop);

        if (minDist == 0f)
            minDist = GetPropMinDist(prop);

        for (int i = 0; i < max; i++)
        {
            if (Random.Range(0, 100) < percentage)
            {
                Vector3 pos = RandomObjectPositionInHex(minRad, maxRad, height);

                int rescuer = 0;
                while (TooCloseToOtherProp(pos, propsPositions, minDist) || TooCloseFromEntrance(pos, hex))
                {
                    if (rescuer > 100 || minDist < 0.2f)
                    {
                        if (prop.name.Contains("Gem"))//we'd need to destroy the objects the gem is colliding with:
                        {
                            List<GameObject> toDestroyObjects = new List<GameObject>();
                            foreach (Transform child in hex)
                            {
                                if (Vector3.Distance(child.position, pos + hex.position) < minDist)
                                {
                                    toDestroyObjects.Add(child.gameObject);
                                }
                            }

                            while (toDestroyObjects.Count != 0)
                            {
                                Destroy(toDestroyObjects[0]);
                                toDestroyObjects.RemoveAt(0);
                            }

                            break;
                        }
                        else
                        {
                            Debug.Log("Rescued from infinite Loop on Creating " + prop.name);
                            return;
                        }
                    }
                    minDist -= 0.05f;
                    pos = RandomObjectPositionInHex(minRad, maxRad, height);
                    rescuer++;
                }

                propsPositions.Add(pos);
                Quaternion rot = Quaternion.identity;
                if (!faceCenter)
                {
                    rot = Quaternion.Euler(0, RandomPoint(180), 0);
                }
                else
                {
                    rot = Quaternion.LookRotation(-pos);
                }

                GameObject p = Instantiate(prop, pos + hex.position, rot, hex.transform);
                prop = props[Random.Range(0, props.Length - 1)];
            }
        }
    }

    private float GetPropHeight(GameObject prop)
    {
        if (prop == m_rock)
            return -0.1f;
        else
            return 0.0f;
    }

    private float GetPropMinDist(GameObject prop)
    {
        if (prop == m_rock)
            return 2.5f;
        else
            return 0.5f;
    }

    private bool TooCloseToOtherProp(Vector3 pos, List<Vector3> propPositions, float minDist)
    {
        foreach(Vector3 propPos in propPositions)
        {
            if (Vector3.Distance(pos, propPos) < minDist)
                return true;
        }
        return false;
    }

    private bool TooCloseFromEntrance(Vector3 pos, Transform hex, float minDist = 2.5f)
    {
        foreach (Transform face in hex)
        {
            if (face.name.Contains("face") && face.GetChild(0).name.Contains("T"))
            {
                if (Vector3.Distance(pos, face.position - hex.position) < minDist)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool NotCloseFromFace(bool active, Vector3 pos, Transform hex, float minDist = 3.5f)
    {
        if (active)
        {
            foreach (Transform face in hex)
            {
                if (face.name.Contains("face") && !face.GetChild(0).name.Contains("T"))
                {
                    if (Vector3.Distance(pos, face.position - hex.position) < minDist)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private static float RandomPoint(float maxPos)
    {
        return (Random.Range(0, 2) * 2 - 1) * Random.Range(0, maxPos);
    }

    private Vector3 RandomObjectPositionInHex(float MIN_RAD, float MAX_RAD, float objHeight)
    {
        Vector3 pos = new Vector3(RandomPoint(Graph.RDist -1f), objHeight, RandomPoint(Graph.RDist -1f));

        while (Vector3.Distance(pos, Vector3.zero) > MAX_RAD || Vector3.Distance(pos, Vector3.zero) < MIN_RAD)
        {
            pos = new Vector3(RandomPoint(MAX_RAD), objHeight, RandomPoint(MAX_RAD));
        }
        return pos;
    }

    public void CreateSpecificProps(GameObject specificProp)
    {
        bool done = false;
        while (!done)
        {
            int rndNode = Random.Range(0, m_graph.nodeList.Count);
            while (m_graph.nodeList[rndNode].isPath)
            {
                rndNode = Random.Range(0, m_graph.nodeList.Count);
            }

            if (m_graph.nodeList[rndNode].m_type != Graph.CaveType.START)
            {
                //select random hex:
                int rndHex = Random.Range(0, m_graph.nodeList[rndNode].hexsList.Count);

                while (m_graph.nodeList[rndNode].hexsList[rndHex].name.Contains("START"))
                {
                    rndHex = Random.Range(0, m_graph.nodeList[rndNode].hexsList.Count);
                }

                if (m_graph.nodeList[rndNode].hexsList[rndHex].name.Contains("Up"))
                {
                    CreateProp(specificProp, m_graph.nodeList[rndNode], m_graph.nodeList[rndNode].hexsList[rndHex], 100, 1, propsPositions, minDist: 1f, minRad: 4f);
                    done = true;
                }
                else
                {
                    CreateProp(specificProp, m_graph.nodeList[rndNode], m_graph.nodeList[rndNode].hexsList[rndHex], 100, 1, propsPositions, minDist: 1f);
                    done = true;
                }
            }
        }
    }

    public void GenerateRocks()
    {
        foreach(Graph.Node n in m_graph.nodeList)
        {
            if (!n.isPath)
            {
                foreach (Transform hex in n.hexsList)
                {
                    List<Vector3> rockPositions = new List<Vector3>();
                    List<Vector3> activeRockPositions = new List<Vector3>();
                    bool noSpace = false;

                    int numVertexs = 0;
                    foreach (Transform child in hex)
                    {
                        if (child.name.Contains("face"))
                        {
                            numVertexs++;
                        }
                        if (numVertexs > 6)
                        {
                            break;
                        }
                    }

                    //VERTEX ROCKS
                    for (int i = 0; i < numVertexs; i++)
                    {
                        foreach (Transform child in hex)
                        {
                            if (child.name.Contains("face"))
                            {
                                if (child.name.Contains((i + 1).ToString()))
                                {
                                    if (child.childCount > 1 && !child.GetChild(1).name.Contains("B"))// && !child.GetChild(1).name.Contains("B"))
                                    {
                                        Vector3 vertexPoint = HexVertexPoint(i);
                                        vertexPoint -= vertexPoint.normalized * 1.0005f;
                                        int numVertexRocks = Random.Range(0, 3);
                                        for (int j = 0; j < numVertexRocks; j++)
                                        {
                                            Vector3 pos = PoissonDiskSampling(rockPositions, activeRockPositions, 0, 1.25f, ref noSpace);
                                            pos += vertexPoint;
                                            pos += new Vector3(0, 0.5f, 0);
                                            if (j > 0)
                                            {
                                                pos += new Vector3(0, Random.Range(0.2f, 1f), 0);
                                            }
                                            InstantiateRock(hex, pos, true);
                                        }

                                        //Debug.Log("instance in hex: " + hex.name + " " + child.name + " " + child.GetChild(1).name);
                                    }
                                    else
                                    {
                                        //Debug.Log("hex not valid: " + hex.name + " " + child.name + " " + child.GetChild(1).name);
                                    }
                                }
                            }
                        }
                    }

                    //WALKABLE ROCKS
                    rockPositions.Clear();
                    activeRockPositions.Clear();
                    noSpace = false;

                    while (!noSpace)
                    {
                        Vector3 pos;
                        if (hex.name.Contains("Up"))
                        {
                            pos = PoissonDiskSampling(rockPositions, activeRockPositions, 3, Graph.RDist - 1.5f, ref noSpace);
                        }
                        else
                        {
                            pos = PoissonDiskSampling(rockPositions, activeRockPositions, 0, Graph.RDist - 1.5f, ref noSpace);
                        }

                        if (!noSpace)
                        {
                            InstantiateRock(hex, pos);
                        }
                    }
                }
            }
        }
    }

    private Vector3 HexVertexPoint(int faceNum)
    {
        switch (faceNum)
        {
            case 0:
                return new Vector3(-Graph.RDist/2, 0f, -Graph.rDist);
            case 1:
                return new Vector3(-Graph.RDist, 0f, 0f);
            case 2:
                return new Vector3(-Graph.RDist/2, 0f, Graph.rDist);
            case 3:
                return new Vector3(Graph.RDist/2, 0f, Graph.rDist);
            case 4:
                return new Vector3(Graph.RDist, 0, 0f);
            case 5:
                return new Vector3(Graph.RDist / 2, 0f, -Graph.rDist);
        }
        return Vector3.zero;
    }

    private void InstantiateRock(Transform hex, Vector3 pos, bool scale = false)
    {
        Quaternion rot = Quaternion.Euler(0, RandomPoint(180), 0);
        int rnd = Random.Range(0, rocks_walkable.Length);
        GameObject prop = rocks_walkable[rnd];
        GameObject rock = Instantiate(prop, pos + hex.position, rot, hex.transform);
        if (scale)
        {
            rock.transform.localScale = new Vector3(rock.transform.localScale.x + Random.Range(0.2f, 0.75f), rock.transform.localScale.y + Random.Range(0.2f, 0.75f), rock.transform.localScale.z + Random.Range(0.2f, 0.75f));
            rock.AddComponent<MeshCollider>();
        }
    }

    private Vector3 PoissonDiskSampling(List<Vector3> rockPositions, List<Vector3> activeRockPositions, float MIN_RAD, float MAX_RAD, ref bool noSpace)
    {
        Vector3 pos = Vector3.zero;
        if (activeRockPositions.Count == 0)
        {
            pos = RandomObjectPositionInHex(MIN_RAD, MAX_RAD, 0);
            activeRockPositions.Add(pos);
            rockPositions.Add(pos);
            return pos;
        }

        bool gotPosition = false;

        while (!gotPosition && activeRockPositions.Count>0)
        {
            Vector3 spawnPoint = activeRockPositions[0];
            int numTries = 0;
            while(numTries < rejectionLimit)
            {
                numTries++;
                pos = spawnPoint + RandomObjectPositionInHex(minRockDist, maxRockDist, 0);

                //comprovar que no xoca amb cap
                gotPosition = true;
                foreach(Vector3 point in rockPositions)
                {
                    if(Vector3.Distance(point, pos) < minRockDist)
                    {
                        gotPosition = false;
                    }
                }

                if(pos.magnitude > MAX_RAD || pos.magnitude < MIN_RAD)
                {
                    gotPosition = false;
                }
            }

            if (numTries >= rejectionLimit)
            {
                activeRockPositions.RemoveAt(0);
            }
        }

        if (!gotPosition)
        {
            if(activeRockPositions.Count == 0)
            {
                int numTries = 0;
                while (numTries < rejectionLimit)
                {
                    numTries++;
                    pos = RandomObjectPositionInHex(MIN_RAD, MAX_RAD, 0);

                    //comprovar que no xoca amb cap
                    gotPosition = true;
                    foreach (Vector3 point in rockPositions)
                    {
                        if (Vector3.Distance(point, pos) < Random.Range(minRockDist, maxRockDist*0.8f))
                        {
                            gotPosition = false;
                        }
                    }
                    if (gotPosition)
                    {
                        rockPositions.Add(pos);
                        activeRockPositions.Add(pos);
                        return pos;
                    }
                }
            }
            
            noSpace = true;
        }
        else
        {
            rockPositions.Add(pos);
            activeRockPositions.Add(pos);
        }

        return pos;
    }

    private void RemoveListElements(List<GameObject> list)
    {
        foreach(GameObject go in list)
        {
            Destroy(go);
        }

        list.Clear();
    }

    #endregion

    #region ExtraCave

    //it returns the list of hexs in order to spawn spiders
    public void CreateFallingPath()
    {
        Graph.Node node = GetFartherNode();
        //first we get the farthest hex in level
        Transform hex = GetFartherHex(node);

        //get the face where there's a tunnel
        Transform face = GetFarthestFace(hex);
        //Transform face = GetHexsFace(hex);

        if (!face)
        {
            Debug.LogError("[FALLING PATH] -> Couldn't find hex face");
            impossibleFloor = true;
            return;
        }

        //FIRST TUNNEL
        GameObject tunnel = Instantiate(emptyCavePrefab, face.position, face.rotation, transform);
        tunnel.name = "Tunnel to Falling Path in Hex " + face.parent.name;
        tunnel.transform.Rotate(new Vector3(180, -90, 0));

        int n = Random.Range(2, 4);
        CreateTunnelToFallingPath(tunnel.transform, n, tunnel.transform);
        TunnelFace(face);
        Vector3 finalPos = face.GetChild(0).position;
        finalPos += face.forward * Graph.rDist * 2 * (n-1);
        GameObject wall = PlaceHugeWall(tunnel.transform, finalPos);
        Transform lastPathHexFace = null;

        node.tunnelList.Add(tunnel);
        node.tunnelHexDict.Add(tunnel, hex.gameObject);

        //ACTUAL PATH
        GameObject path = Instantiate(emptyCavePrefab, finalPos, Quaternion.identity, transform);
        path.name = "PATH";
        CreatePath(tunnel.transform, finalPos, ref lastPathHexFace, path.transform, wall.transform);//lastPathHexFace is given inside
        if (!lastPathHexFace)
        {
            Debug.LogError("[FALLING PATH] -> Couldn't get lastPathHexFace");
            return;
        }

        Graph.Node pathNode = new Graph.Node(path);
        pathNode.adjacency.Add(node);
        node.adjacency.Add(pathNode);
        pathNode.tunnelList.Add(tunnel);
        pathNode.tunnelHexDict.Add(tunnel, path);
        pathNode.mass = 10;
        pathNode.isPath = true;
        pathNode.lastPathHex = lastPathHexFace.parent;
        m_graph.nodeList.Add(pathNode);

        //FINAL TUNNEL
        GameObject finalTunnel = Instantiate(emptyCavePrefab, lastPathHexFace.position, lastPathHexFace.rotation*Quaternion.Euler(new Vector3(0,0,90)), transform);
        //finalTunnel.transform.Rotate(new Vector3(180, 180, 0));

        //Debug.LogError(lastPathHexFace.name);

        finalTunnel.name = "Final Tunnel to ExtraCave ";
        wall = PlaceHugeWall(finalTunnel.transform, lastPathHexFace.position);
        wall.transform.Rotate(new Vector3(0, -180, 180));

        pathNode.tunnelList.Add(finalTunnel);
        pathNode.tunnelHexDict.Add(finalTunnel, path);

        int n2 = Random.Range(2, 4);
        CreateTunnelToFallingPath(finalTunnel.transform, n2, finalTunnel.transform);

        //EXTRA CAVE
        GameObject extraCave = Instantiate(emptyCavePrefab, finalTunnel.transform.position, Quaternion.identity, transform);
        extraCave.name = "EXTRA_CAVE";
        finalPos = finalTunnel.transform.position + finalTunnel.transform.right * Graph.rDist * 2 * (n2-1);
        CreateExtraCave(finalTunnel.transform, finalPos, extraCave.transform);

        Graph.Node extraCaveNode = new Graph.Node(extraCave);
        extraCaveNode.adjacency.Add(pathNode);
        pathNode.adjacency.Add(extraCaveNode);
        extraCaveNode.tunnelList.Add(finalTunnel);

        extraCaveNode.tunnelHexDict.Add(finalTunnel, extraCaveNode.hexsList[0].gameObject);
        m_graph.nodeList.Add(extraCaveNode);
    }

    private void CreateTunnelToFallingPath(Transform tunnel, int numTunnels, Transform parent)
    {
        Vector3 startPos = tunnel.transform.position;
        int numPart = 0;

        //cap A:
        GameObject tunnelPart = Instantiate(tunnel_Cap_A_Prefab, startPos, tunnel.rotation, parent);
        tunnelPart.name = "Tunnel Cap A " + numPart;
        numPart++;

        //mid:
        while (numPart < numTunnels - 1)
        {
            tunnelPart = Instantiate(tunnel_mid_forward, startPos, tunnel.rotation, parent);

            tunnelPart.name = "Tunnel Part " + numPart;
            numPart++;

            startPos += tunnelPart.transform.right * Graph.rDist * 2;

            //block?¿
            //CreateTunnelBlockers(tunnelPart.transform);
        }

        startPos += tunnelPart.transform.right * Graph.rDist*2;
        //cap B:
        tunnelPart = Instantiate(tunnel_Cap_B_Prefab, startPos, tunnel.rotation, parent);
        tunnelPart.name = "Tunnel Cap B " + numPart;
    }

    private Graph.Node GetFartherNode()
    {
        float fartherDistance = 0;
        //Graph.Node fartherNode = null;
        Graph.Node fartherNode = m_graph.nodeList[0];
        //get farther cave
        foreach (Graph.Node n in m_graph.nodeList)
        {
            if(Vector3.Distance(n.position, transform.position) >= fartherDistance)
            {
                fartherDistance = Vector3.Distance(n.position, transform.position);
                fartherNode = n;
            }
        }

        return fartherNode;
    }

    private Transform GetFartherHex(Graph.Node fartherNode)
    {
        float fartherDistance = 0;
        //Transform fartherHex = null;
        Transform fartherHex = fartherNode.hexsList[0];
        //get farther hex:
        foreach (Transform hex in fartherNode.hexsList)
        {
            if (Vector3.Distance(hex.position, transform.position) >= fartherDistance)
            {
                fartherDistance = Vector3.Distance(hex.position, transform.position);
                fartherHex = hex;
            }
        }

        return fartherHex;
    }

    private Transform GetFarthestFace(Transform hex)
    {
        float fartherDistance = 0;
        Transform farthestFace = null;
        foreach (Transform face in hex)
        {
            if (face.name.Contains("face"))
            {
                if (Vector3.Distance(face.position, transform.position) >= fartherDistance)
                {
                    fartherDistance = Vector3.Distance(face.transform.position, transform.position);
                    farthestFace = face.transform;
                }
            }
        }

        return farthestFace;
    }

    private Transform GetHexsFace(Transform hex)
    {
        foreach (Transform child in hex)
        {
            if (child.name.Contains("face"))
            {
                if (!DetectObstructionInDirection(child)) // && !Graph.DetectTunnelObstruction (amb edges?¿) TODO
                {
                    return child;
                }
            }
        }
        return null;
    }

    private bool DetectObstructionInDirection(Transform face)
    {
        foreach(GameObject cave in cavesList)
        {
            foreach(Transform hex in cave.transform)
            {
                if ((hex.position - face.parent.transform.position).normalized == face.forward)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private GameObject PlaceHugeWall(Transform tunnel, Vector3 pos)
    {
        GameObject wall = Instantiate(hugeWallPrefab, pos, tunnel.rotation, tunnel);
        wall.transform.Rotate(new Vector3(-180,-90,0));
        return wall;
    }

    private void CreatePath(Transform tunnel, Vector3 pos, ref Transform lastPathHexFace, Transform parent, Transform wall)//lastPathHex is not really used inside, it's only to get it for the next function
    {
        Vector3 initPos = pos;
        //first hex
        pos += tunnel.right * 1.25f*Mathf.Sin(60 * Mathf.Deg2Rad) *2;
        Quaternion rot = tunnel.rotation;
        //rot *= Quaternion.Euler(90, 0, 0);
        GameObject pathHex = Instantiate(fallingPathHexPrefab, pos, rot, parent);
        foreach (Transform face in pathHex.transform)
        {
            if (face.transform.name.Contains("face"))
            {
                float angle = Vector3.Angle(-face.right, tunnel.right);
                if (angle > 120)
                {
                    //DestroyImmediate(face.gameObject);
                    //DebugTools.DebugDestroyObject(gameObject.name, face.gameObject);
                }
            }
        }

        //variables
        List<GameObject> lastPathHexList = new List<GameObject>();
        List<Transform> totalFacesToCut = new List<Transform>();
        pathHexList.Add(pathHex);
        lastPathHexList.Add(pathHex);

        int numPathHexs = 0;
        int maxPathHexs = Random.Range(50,100);

        //main loop
        while (numPathHexs < maxPathHexs)
        {
            bool impossiblePathHex = true;
            int rndNum = 0;
            int iterationsOnGettingPosition = 0;

            //GET POSITION
            while (impossiblePathHex && iterationsOnGettingPosition < 100)
            {
                iterationsOnGettingPosition++;
                impossiblePathHex = false;

                //get all faces from a random pathHex
                rndNum = Random.Range(0, lastPathHexList.Count);

                List<Transform> faceList = GetFacesList(lastPathHexList[rndNum].transform, tunnel.right);

                //get a new position from a random face
                int rndFace = Random.Range(0, faceList.Count);
                pos = lastPathHexList[rndNum].transform.position + faceList[rndFace].up * 5f * Mathf.Sin(60 * Mathf.Deg2Rad);

                if (!CheckPosInCorrectSideOfPlane(pos, wall.position, wall.forward))
                //if (!CheckPosInCorrectSideOfPlane(pos, parent.position, parent.forward))
                {
                    impossiblePathHex = true;
                    //Debug.LogError("[FALLING PATH] is behind wall");
                }
                //check if the position is already occupied
                else if (CheckPositionInList(pathHexList, pos))
                {
                    impossiblePathHex = true;
                    //Debug.LogError("[FALLING PATH] got same position with another hex");
                }
            }

            if (iterationsOnGettingPosition >= 99)
            {
                Debug.LogError("[FALLING PATH] -> Problem iterating for a new position on creating FALLINGPATH");
                return;
            }

            GameObject pathHex2 = Instantiate(fallingPathHexPrefab, pos, rot, parent);
            pathHex2.transform.name = "PathHex " + numPathHexs;
            pathHexList.Add(pathHex2);
            lastPathHexList.Add(pathHex2);

            if(lastPathHexList.Count> 4)
            {
                //lastPathHexList.RemoveAt(Random.Range(0, lastPathHexList.Count - 1));
                lastPathHexList.RemoveAt(0);
            }

            //cutwalls
            List<Transform> facesToCut = new List<Transform>();
            foreach (GameObject path in pathHexList)
            {
                if (path != pathHex2 && Vector3.Distance(path.transform.position, pathHex2.transform.position) < Graph.rDist)
                {
                    foreach (Transform face in pathHex2.transform)
                    {
                        if (face.transform.name.Contains("face"))
                        {
                            foreach (Transform face2 in path.transform)
                            {
                                if (face2.transform.name.Contains("face"))
                                {
                                    //Debug.LogError("in");
                                    if (!totalFacesToCut.Contains(face2))
                                    {
                                        if (Vector3.Distance(face.position, face2.position) < 0.1f)
                                        {
                                            facesToCut.Add(face);
                                            facesToCut.Add(face2);
                                            totalFacesToCut.Add(face);
                                            totalFacesToCut.Add(face2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            while (facesToCut.Count != 0)
            {
                Destroy(facesToCut[0].gameObject);
                facesToCut.RemoveAt(0);
                //Debug.LogError("removing");
            }

            numPathHexs++;
        }

        //get the farthest pathHex:
        float distance = 0;
        foreach (GameObject path in pathHexList)
        {
            float d = Vector3.Distance(path.transform.position, initPos);
            if ( d > distance)
            {
                distance = d;
                lastPathHexFace = path.transform;
            }
        }

        Transform f = null;
        //get its farthest and undestroyed face:
        distance = 0;
        foreach(Transform face in lastPathHexFace)
        {
            if (face.transform.name.Contains("face"))
            {
                float d = Vector3.Distance(face.position, initPos);
                if (d > distance && !totalFacesToCut.Contains(face))
                {
                    distance = d;
                    f = face;
                }
            }
        }

        lastPathHexFace = f;
    }

    private bool CheckPosInCorrectSideOfPlane(Vector3 pos, Vector3 planePos, Vector3 planeNormal)
    {
        Vector3 dir = pos - planePos;
        dir.Normalize();
        //Debug.Log("dir: " + dir);
        //Debug.Log("planeNormal: " + planeNormal);
        float angle = Vector3.Angle(dir, planeNormal);
        if(angle >= 90)
        {
            return false;
        }
        return true;
    }

    private bool CheckPositionInList(List<GameObject> list, Vector3 pos)
    {
        foreach(GameObject item in list)
        {
            if (Vector3.Distance(item.transform.position, pos) < .1f)
                return true;
        }

        return false;
    }

    private List<Transform> GetFacesList(Transform pathHex, Vector3 dir)
    {
        List<Transform> faceList = new List<Transform>();
        foreach (Transform face in pathHex)
        {
            if (face.transform.name.Contains("face"))
            {
                float angle = Vector3.Angle(-face.right, dir);
                if (angle > 60)
                {
                    faceList.Add(face);
                }
            }
        }
        return faceList;
    }

    private void CreateExtraCave(Transform tunnel, Vector3 tunnelPos, Transform parent)
    {
        GameObject cave = parent.gameObject;
        cavesList.Clear();
        cavesList.Add(cave);
        GameObject hex = Instantiate(HexPrefab, tunnelPos + tunnel.right* Graph.rDist , HexPrefab.transform.rotation, cave.transform);
        GameObject hexInit = hex;
        List<GameObject> hexsList = new List<GameObject>();
        hexsList.Add(hex);

        //create extra cave neighbors:
        int maxNumCaves =  Random.Range(2, 6);
        int numCaves = 0;

        while(numCaves < maxNumCaves)
        {
            hex = cavesList[0].transform.GetChild(Random.Range(0, cavesList[0].transform.childCount - 1)).gameObject;
            foreach(Transform face in hex.transform)
            {
                float angle = Vector3.Angle(-face.forward, tunnel.right);
                Vector3 pos = face.position + face.forward * Graph.rDist;

                if (!CheckPositionInList(hexsList, pos) && angle > 60)
                {
                    hex = Instantiate(HexPrefab, pos, HexPrefab.transform.rotation, cave.transform);
                    hexsList.Add(hex);
                    numCaves++;
                    break;
                }
            }
        }

        //cut the walls
        CutWalls();

        //tunnel the entering face
        foreach (Transform face in hexInit.transform)
        {
            if (-face.forward == tunnel.right)
            {
                TunnelFace(face);
                break;
            }
        }

        //place the recompensation:
        hex = hexsList[Random.Range(0, hexsList.Count - 1)];
        Vector3 pos1 = hex.transform.position + new Vector3(0,8.19f, 0);
    }

    #endregion

    #region Debug Methods

    //***********(PROVISIONAL)******************
    private GameObject TunnelConnector(GameObject m_tunnel, Vector3 pos1, Vector3 pos2)
    {
        //put the cylinder tunnel facing the correct direction and scale
        m_tunnel.transform.position = pos1;
        m_tunnel.transform.LookAt(pos2);
        Vector3 localScale = m_tunnel.transform.localScale;
        localScale.z = (pos2 - pos1).magnitude / 2f;
        m_tunnel.transform.localScale = localScale;
        //Instantiate(m_tunnel, beginHole.transform.parent.GetChild(0).transform, true);
        return Instantiate(m_tunnel);
    }
    #endregion
}
