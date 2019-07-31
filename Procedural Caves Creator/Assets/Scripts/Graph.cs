using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph: MonoBehaviour
{
    private static int numNodes;
    public List<Node> nodeList;
    public List<Edge> edgeList;
    private const float MAX_NEIGHBOR_DISTANCE = 70f;
    private List<Transform> occupiedHoles;
    private static List<Node> floorConnectionNodes;
    public bool connectedNodes;
    private bool isLastFloor;

    //same variables as in SimpleHexLevel.cs
    public static float RDist = 10f;
    public static float rDist = RDist * Mathf.Sin(60f * Mathf.Deg2Rad);
    public static float diagDist = 7.500001f;

    private GameObject help;


    public enum CaveType
    {
        ROCKY = 1,
        NORMAL = 2, //a bit of everything (?)
        MINE = 3,

        //this ones are given
        START,
        UPDOWN_CONNECTION,
        TRAP,
        COMBINED,
    }

    public class Node//nodes are SimpleHexlevel's Caves.
    {
        //cave info
        public int number { get; private set; }
        public Vector3 position;
        public int mass;//number of hexes
        public GameObject m_mesh;
        public bool isFloorConnection;
        public CaveType m_type;
        public bool hasTurret;
        public bool isPath;
        public Transform lastPathHex;

        //data lists
        public List<Node> adjacency { get; private set; }
        public List<Transform> facesList;
        public List<Transform> hexsList;
        public List<Edge> smallForwardPossibleHoles;
        public List<Edge> bigForwardPossibleHoles;
        public List<Edge> smallDiagPossibleHoles;
        public List<Edge> bigDiagPossibleHoles;
        public List<GameObject> tunnelList;
        public List<Edge> edgeList;

        //dictionaries
        public Dictionary<Transform, bool> scannedHexsDict;
        public Dictionary<GameObject, GameObject> hexsConnectionDict;
        public Dictionary<GameObject, GameObject> tunnelHexDict;

        //enemies
        public int numArakkag;
        public int numHammer;


        public Node(int n, GameObject mesh)
        {
            number = n;
            m_mesh = mesh;
            position = mesh.transform.position;
            mass = mesh.transform.childCount;
            isFloorConnection = DetectFloorConnection();

            //lists
            adjacency = new List<Node>();
            hexsList = new List<Transform>();
            GetHexes();
            facesList = new List<Transform>();
            GetFaces();
            smallForwardPossibleHoles = new List<Edge>();
            bigForwardPossibleHoles = new List<Edge>();
            smallDiagPossibleHoles = new List<Edge>();
            bigDiagPossibleHoles = new List<Edge>();
            scannedHexsDict = new Dictionary<Transform, bool>();
            InitializeScannedHexs();
            hexsConnectionDict = new Dictionary<GameObject, GameObject>();
            tunnelList = new List<GameObject>();
            edgeList = new List<Edge>();
            tunnelHexDict = new Dictionary<GameObject, GameObject>();

            m_type = SetCaveType();
            SetCaveName();

            hasTurret = false;
        }

        public Node(GameObject mesh)
        {
            m_mesh = mesh;
            position = mesh.transform.position;
            mass = mesh.transform.childCount;

            //lists
            adjacency = new List<Node>();
            hexsList = new List<Transform>();
            GetHexes();

            scannedHexsDict = new Dictionary<Transform, bool>();
            InitializeScannedHexs();

            edgeList = new List<Edge>();
            tunnelList = new List<GameObject>();
            tunnelHexDict = new Dictionary<GameObject, GameObject>();

            hasTurret = false;
        }

        private void GetHexes()
        {
            foreach(Transform hex in m_mesh.transform)
            {
                hexsList.Add(hex);
            }
        }

        private void GetFaces()
        {
            foreach(Transform hex in hexsList)
            {
                foreach(Transform face in hex)
                {
                    if(face.gameObject.activeSelf)
                        facesList.Add(face);
                }
            }
        }

        public int getDegree()
        {
            return adjacency.Count;
        }

        private bool DetectFloorConnection()
        {
            bool connection = false;
            foreach(Transform hex in m_mesh.transform)
            {
                if (hex.name.Contains("Connection"))
                {
                    floorConnectionNodes.Add(this);
                    connection =  true;
                }
            }
            return connection;
        }

        private void InitializeScannedHexs()
        {
            foreach(Transform hex in hexsList)
            {
                scannedHexsDict.Add(hex, false);
            }
        }

        private CaveType SetCaveType()
        {
            //START / GOAL
            foreach (Transform hex in hexsList)
            {
                if (hex.name.Contains("START"))
                {
                    if (isFloorConnection)
                        return CaveType.COMBINED;
                    else
                        return CaveType.START;
                }    
            }

            //FLOOR CONNECTION
            if (isFloorConnection)
            {
                int numConnectedHexs = 0;
                CaveType type = CaveType.UPDOWN_CONNECTION;//initialize
                foreach(Transform hex in hexsList)
                {
                    if (hex.name.Contains("UpDown"))
                    {
                        type = CaveType.UPDOWN_CONNECTION;
                        numConnectedHexs++;
                    }
                    else if (hex.name.Contains("Trap"))
                    {
                        type = CaveType.TRAP;
                        numConnectedHexs++;
                    }
                }

                if (numConnectedHexs == 1)
                    return type;
                else
                {
                    //Debug.Log("COMBINED! "  + m_mesh.name + " " + numConnectedHexs);
                    return CaveType.COMBINED;
                }
            }

            if(mass > 2)//big cave
            {
                if (mass > 4)//baixo la possibilitat en coves enormes, impossibles de superar.
                {
                    if (Random.Range(0, 100) < 25)//coves enormes de Ygz
                    {
                        return CaveType.MINE;
                    }
                }
                else
                {
                    if (Random.Range(0, 100) < 15)//coves enormes de Ygz
                    {
                        return CaveType.MINE;
                    }
                }
            }
            else //small cave
            {
                if (Random.Range(0, 100) < 25)//25%
                {
                    return CaveType.MINE;
                }
            }

            int x = Random.Range(1,3); //normals o rocoses
            return (CaveType)x;
        }

        private void SetCaveName()
        {
            m_mesh.transform.name += " " + m_type.ToString();
        }
    }

    public class Edge//edges are the whole tunnel
    {
        public Node node1 { get; private set; }
        public Node node2 { get; private set; }
        public Transform face1;
        public Transform face2;
        public float weight { get; private set; }

        public GameObject mesh;
        public bool isForward;
        public float distance;

        public Edge(Node n1, Node n2, Transform h1, Transform h2, float w, bool forward)
        {
            node1 = n1;
            node2 = n2;
            face1 = h1;
            face2 = h2;
            weight = w;
            isForward = forward;


            mesh = null;
            distance = Vector3.Distance(face1.position, face2.position);
        }

        public bool Contains(Node n)
        {
            if (node1 == n ||  node2 == n)
                return true;
            return false;
        }

        public bool IntersectsWithEdge(Edge edge)
        {
            return DoIntersect(face1.position, face2.position, edge.face1.position, edge.face2.position);
        }
    }

    public void CreateGraph(List<GameObject> cavesList, bool isLast = false, GameObject _help = null)
    {
        nodeList = new List<Node>();
        edgeList = new List<Edge>();
        floorConnectionNodes = new List<Node>();
        numNodes = 0;
        CreateNodes(cavesList);
        isLastFloor = isLast;
        connectedNodes = true; // it's only false when "hasPathBFS is false betwween both floor connections

        help = _help;
    }

    private void CreateNodes(List<GameObject> cavesList)
    {
        for (int i = 0; i < cavesList.Count; i++)
        {
            nodeList.Add(new Node(i, cavesList[i]));
        }
    }

    public void CreateEdges()
    {
        occupiedHoles = new List<Transform>();

        //connect random small forward
        foreach (Node n in nodeList )
        {
            List<Edge> copySmallForwardPossibleHoles = n.smallForwardPossibleHoles;
            AddAdjacentEdges(copySmallForwardPossibleHoles);
        }

        //check graph connection
        if (isConnectedGraphBFS(nodeList[0]))
        {
            //Debug.Log("exit after small forward " + transform.name);
            return;
        }

        //connect random small diag
        foreach (Node n in nodeList)
        {
            List<Edge> copySmallDiagPossibleHoles = n.smallDiagPossibleHoles;
            AddAdjacentEdges(copySmallDiagPossibleHoles);
        }

        //check graph connection
        if (isConnectedGraphBFS(nodeList[0]))
        {
            //Debug.Log("exit after small diag " + transform.name);
            return;
        }

        //connect random big forward
        foreach (Node n in nodeList)
        {
            List<Edge> copyBigForwardPossibleHoles = n.bigForwardPossibleHoles;
            n.bigForwardPossibleHoles = SortPossibleHoles(copyBigForwardPossibleHoles);
            AddAdjacentEdges(n.bigForwardPossibleHoles);

            //check graph connection
            if (isConnectedGraphBFS(nodeList[0]))//leave once it's connected
            {
                Debug.Log(" [GRAPH GENERATION ] exit after big forward " + transform.name);
                return;
            }
        }

        //RemoveDisconnectedNodesBFS(nodeList[0]);
        //return;

        //connect random big diagonal
        foreach (Node n in nodeList)
        {
            List<Edge> copyBigDiagPossibleHoles = n.bigDiagPossibleHoles;
            n.bigDiagPossibleHoles = SortPossibleHoles(copyBigDiagPossibleHoles);
            AddAdjacentEdges(n.bigDiagPossibleHoles);

            //check graph connection
            if (isConnectedGraphBFS(nodeList[0])) //leave once it's connected
            {
                Debug.Log(" [GRAPH GENERATION ] exit after big diag " + transform.name);
                return;
            }
        }

        if (!isLastFloor)
        {
            //check beginning to next floor connection
            foreach (Node n in floorConnectionNodes)
            {
                if (!hasPathBFS(nodeList[0], n))
                {
                    //WE NEED TO REPEAT
                    Debug.Log("[GRAPH GENERATION ] NO PATH!");
                    connectedNodes = false;
                    return;
                }
            }
        }

        RemoveDisconnectedNodesBFS(nodeList[0]);
    }

    public void GetPossibleConnections()
    {
        List<Node> checkedNodes = new List<Node>();//els que ja han passat no tenen pq tornar-hi, per optimització
        foreach (Node n1 in nodeList)
        {
            foreach (Transform f1 in n1.facesList)
            {
                foreach (Node n2 in nodeList)
                {
                    if (n1 != n2 && Vector3.Distance(n1.position, n2.position) < MAX_NEIGHBOR_DISTANCE && !checkedNodes.Contains(n2))
                    {
                        foreach (Transform f2 in n2.facesList)
                        {
                            Vector3 vectorDir = f2.position - f1.position;
                            vectorDir = vectorDir.normalized;
                            float angleVector = Vector3.Angle(f1.forward, vectorDir);
                            float angleForwards = Vector3.Angle(f1.forward, f2.forward);
                            //forward
                            if (angleVector == 0 && f2.forward != f1.forward )
                            {
                                float distance = Vector3.Distance(f1.position, f2.position);
                                if (distance <= rDist * 2 + 1f)//small
                                {
                                    n1.smallForwardPossibleHoles.Add(new Edge(n1, n2, f1, f2, 1, true));
                                    n2.smallForwardPossibleHoles.Add(new Edge(n2, n1, f2, f1, 1, true));
                                }
                                else //BIG
                                {
                                    n1.bigForwardPossibleHoles.Add(new Edge(n1, n2, f1, f2, distance/(rDist*2), true));
                                    n2.bigForwardPossibleHoles.Add(new Edge(n2, n1, f2, f1, distance/(rDist*2), true));
                                }
                            }
                            //diagonal
                            else if (angleVector <= 30 + 1f && angleVector > 30f -1f && angleForwards > 120f - 1f && angleForwards <= 120 + 1f && f1.forward!=f2.forward)
                            {
                                float distance = Vector3.Distance(f1.position, f2.position);

                                if (distance <= diagDist * 2 +1f)//small
                                {
                                    n1.smallDiagPossibleHoles.Add(new Edge(n1, n2, f1, f2, 1, false));
                                    n2.smallDiagPossibleHoles.Add(new Edge(n2, n1, f2, f1, 1, false));
                                }
                                else //BIG
                                {
                                    if(distance < diagDist * 2 * 5)
                                    {
                                        n1.bigDiagPossibleHoles.Add(new Edge(n1, n2, f1, f2, distance / (diagDist * 2), false));
                                        n2.bigDiagPossibleHoles.Add(new Edge(n2, n1, f2, f1, distance / (diagDist * 2), false));
                                    }
                                }
                            }

                            //DEBUG
                            //if (h1.parent.name == "face1" && h2.parent.name == "face3")
                            //{
                            //    Debug.Log(h1.parent.parent.name + " " + h1.parent.name + " -- " + h2.parent.parent.name + " " + h2.parent.name + "angle Vector: " + Vector3.Angle(h1.forward, vectorDir) + " angle: " + Vector3.Angle(h1.forward, h2.forward));
                            //    Debug.Log(Vector3.Angle(h1.forward, vectorDir));
                            //    Debug.Log(Vector3.Angle(h1.forward, h2.forward));
                            //    Debug.Log(Vector3.Distance(h1.position, h2.position));
                            //}

                            //Debug.Log(h1.parent.parent.name + " " + h1.parent.name);

                            //if(h1.parent.parent.name == "Hex 1")
                            //{
                            //    Debug.Log("Hex 1 " + h2.parent.parent.name + " " + h2.parent.name);
                            //}
                            //if (h1.parent.parent.name == "Hex 1" && h2.parent.parent.name == "Hex 2")
                            //{
                            //    if (h1.parent.name == "face4" && h2.parent.name == "face6")
                            //    {
                            //        Debug.Log("angle Vector: " + Vector3.Angle(h1.forward, vectorDir) + " angle: " + Vector3.Angle(h1.forward, h2.forward));
                            //    }
                            //}
                        }
                    }
                }
            }
            checkedNodes.Add(n1);
        }
    }

    private void AddAdjacentEdges(List<Edge> copyList)
    {
        int cut = 0;
        while (copyList.Count != 0 && cut < 1000)//mentres quedin possibilitats de crear un túnel
        {
            cut++;
            int num = Random.Range(0, copyList.Count - 1);
            Edge e = copyList[num];
            if (occupiedHoles.Contains(e.face1) || occupiedHoles.Contains(e.face2) || e.node1.adjacency.Contains(e.node2))//mirem si ja estan ocupats o ja estan connectats
            {
                copyList.RemoveAt(num);
            }
            else if (CheckEdgeIntersection(e) || DetectTunnelObstruction(e))
            {
                copyList.RemoveAt(num);
            }
            else
            {
                e.node1.adjacency.Add(e.node2);
                e.node2.adjacency.Add(e.node1);
                edgeList.Add(e);
                occupiedHoles.Add(e.face1);
                occupiedHoles.Add(e.face2);
                copyList.Clear();//per sortir del loop
            }
        }

        if (cut >= 900)
            Debug.Log("ERROR in Graph.AddAdjacentEdges");
    }

    private bool CheckEdgeIntersection(Edge e)
    {
        foreach(Edge edge in edgeList)
        {
            if (e.IntersectsWithEdge(edge))
                return true;
        }

        return false;
    }

    private List<Edge> SortPossibleHoles(List<Edge> list)
    {
        List<Edge> sorted = new List<Edge>();
        int cut = 0;
        while (list.Count != 0 && cut< 1000)
        {
            cut++;
            Edge smaller = list[0];
            foreach(Edge e in list)
            {
                if (smaller.distance > e.distance)
                    smaller = e;
            }
            list.Remove(smaller);
            sorted.Add(smaller);
        }
        if (cut >= 900)
            Debug.Log("ERROR in Graph.SortPossibleHoles");

        return sorted;
    }

    private bool isConnectedGraphBFS(Node source)
    {
        HashSet<Node> visited = VisitedNodesBFS(source);

        if (visited.Count == nodeList.Count)
            return true;
        else
            return false;
    }

    private void RemoveDisconnectedNodesBFS(Node source)
    {
        HashSet<Node> visited = VisitedNodesBFS(source);
        HashSet<Node> deleteNodes = new HashSet<Node>();
        HashSet<Edge> deleteEdges = new HashSet<Edge>();

        //check visited nodes
        foreach (Node n in nodeList)
        {
            if (!visited.Contains(n))
            {
                Destroy(n.m_mesh);
                deleteNodes.Add(n);
            }
        }

        //delete nodes
        foreach(Node n in deleteNodes)
        {
            nodeList.Remove(n);
            foreach(Edge e in edgeList)
            {
                if (e.Contains(n))
                    deleteEdges.Add(e);
            }
        }

        //delete edges
        foreach(Edge e in deleteEdges)
        {
            edgeList.Remove(e);
        }
    }

    private HashSet<Node> VisitedNodesBFS(Node source)
    {
        Queue<Node> frontier = new Queue<Node>();
        HashSet<Node> visited = new HashSet<Node>();

        frontier.Enqueue(source);
        visited.Add(source);

        while (frontier.Count != 0)
        {
            foreach (Node neighbor in frontier.Dequeue().adjacency)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }    
            }
        }

        return visited;
    }

    private bool hasPathBFS(Node source, Node goal)
    {
        Queue<Node> frontier = new Queue<Node>();
        HashSet<Node> visited = new HashSet<Node>();

        frontier.Enqueue(source);
        visited.Add(source);

        while (frontier.Count != 0)
        {
            foreach (Node neighbor in frontier.Dequeue().adjacency)
            {
                if (neighbor == goal)
                    return true;
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                    //Debug.Log(neighbor.m_mesh.transform.name);
                }
            }
        }

        return false;
    }

    public void ConnectVerticalHexs(List<GameObject> levelHexs, List<GameObject> otherHexs)
    {
        for(int i = 0; i< levelHexs.Count; i++)
        {
            foreach(Node n in nodeList)
            {
                if (n.hexsList.Contains(levelHexs[i].transform))
                {
                    n.hexsConnectionDict.Add(levelHexs[i],otherHexs[i]);
                    
                    //add tunnel vertical connection:
                    foreach(Transform child in levelHexs[i].transform)
                    {
                        if (child.name.Contains("Tunnel"))
                        {
                            n.tunnelList.Add(child.gameObject);
                        }
                    }
                }
            }
        }
    }

    private bool DetectTunnelObstruction(Edge edge)
    {
        Vector3 dir = edge.face2.position - edge.face1.position;
        dir.Normalize();
        Vector3 pos = edge.face1.position;

        if (edge.distance > rDist*2)
        {
            pos += dir * rDist * 2;

            foreach (Node n in nodeList)
            {
                if (n != edge.node1 && n != edge.node2)
                {
                    //Instantiate(help, pos, Quaternion.identity, transform);
                    foreach (Transform hex in n.hexsList)
                    {
                        for (int i = 0; i <= edge.weight*4; i++)
                        {
                            if (Vector3.Distance(pos, hex.position) < rDist + 0.5f)
                            {
                                return true;
                            }
                            pos += dir * rDist/ 2;
                            //Instantiate(help, pos, Quaternion.identity, transform);
                        }
                        pos = edge.face1.position;
                    }
                }
                else
                {
                    foreach (Transform hex in n.hexsList)
                    {
                        if (hex != edge.face1.parent && hex != edge.face2.parent)
                        {
                            //Instantiate(help, pos, Quaternion.identity, transform);
                            for (int i = 0; i <= edge.weight*4; i++)
                            {
                                foreach(Transform f in hex)
                                {
                                    if (Vector3.Distance(pos, f.position) < rDist + 0.5f)
                                    {
                                        return true;
                                    }
                                }
                                
                                pos += dir * rDist/2;
                                //Instantiate(help, pos, Quaternion.identity, transform);
                            }
                            pos = edge.face1.position;
                        }
                    }
                }
            }
        }
        
        return false;
    }

    private static bool OnSegment(Vector3 p, Vector3 q, Vector3 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && q.y <= Mathf.Max(p.z, r.z) && q.z >= Mathf.Min(p.z, r.z))
            return true;

        return false;
    }

    private static int Orientation(Vector3 p, Vector3 q, Vector3 r)
    {
        float val = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);

        if (val == 0) return 0;  // colinear 

        return (val > 0) ? 1 : 2; // clock or counterclock wise 
    }

    private static bool DoIntersect(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        // Find the four orientations needed for general and 
        // special cases 
        int o1 = Orientation(p1, q1, p2);
        int o2 = Orientation(p1, q1, q2);
        int o3 = Orientation(p2, q2, p1);
        int o4 = Orientation(p2, q2, q1);

        // General case 
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases 
        // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases 
    }
}
