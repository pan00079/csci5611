//You will only be turning in this file
//Your solution will be graded based on it's runtime (smaller is better),
//the optimality of the path you return (shorter is better), and the
//number of collisions along the path (it should be 0 in all cases).

//You must provide a function with the following prototype:
// ArrayList<Integer> planPath(Vec2 startPos, Vec2 goalPos, Vec2[] centers, float[] radii, int numObstacles, Vec2[] nodePos, int numNodes);
// Where:
//    -startPos and goalPos are 2D start and goal positions
//    -centers and radii are arrays specifying the center and radius of obstacles
//    -numObstacles specifies the number of obstacles
//    -nodePos is an array specifying the 2D position of roadmap nodes
//    -numNodes specifies the number of nodes in the PRM
// The function should return an ArrayList of node IDs (indexes into the nodePos array).
// This should provide a collision-free chain of direct paths from the start position
// to the position of each node, and finally to the goal position.
// If there is no collision-free path between the start and goal, return an ArrayList with
// the 0'th element of "-1".

// Your code can safely make the following assumptions:
//   - The function connectNeighbors() will always be called before planPath()
//   - The variable maxNumNodes has been defined as a large static int, and it will
//     always be bigger than the numNodes variable passed into planPath()
//   - None of the positions in the nodePos array will ever be inside an obstacle
//   - The start and the goal position will never be inside an obstacle

// There are many useful functions in CollisionLibrary.pde and Vec2.pde
// which you can draw on in your implementation. Please add any additional
// functionality you need to this file (PRM.pde) for compatabilty reasons.

// Here we provide a simple PRM implementation to get you started.
// Be warned, this version has several important limitations.
// For example, it uses BFS which will not provide the shortest path.
// Also, it (wrongly) assumes the nodes closest to the start and goal
// are the best nodes to start/end on your path on. Be sure to fix
// these and other issues as you work on this assignment. This file is
// intended to illustrate the basic set-up for the assignmtent, don't assume
// this example funcationality is correct and end up copying it's mistakes!).


// global variables
// 
static float distanceLimit = 500;

//Here, we represent our graph structure as a neighbor list \
//You can use any graph representation you like
ArrayList<Integer>[] neighbors = new ArrayList[maxNumNodes];  //A list of neighbors can can be reached from a given node
//We also want some help arrays to keep track of some information about nodes we've visited
Boolean[] visited = new Boolean[maxNumNodes]; //A list which store if a given node has been visited
int[] parent = new int[maxNumNodes]; //A list which stores the best previous node on the optimal path to reach this node
float[] distances = new float[maxNumNodes]; //A list which stores shortest distance from start to this node

//Set which nodes are connected to which neighbors (graph edges) based on PRM rules
void connectNeighbors(Vec2[] centers, float[] radii, int numObstacles, Vec2[] nodePos, int numNodes) {
  for (int i = 0; i < numNodes; i++) {
    neighbors[i] = new ArrayList<Integer>();  //Clear neighbors list
    for (int j = 0; j < numNodes; j++) {
      if (i == j) continue; //don't connect to myself
      Vec2 dir = nodePos[j].minus(nodePos[i]).normalized();
      float distBetween = nodePos[i].distanceTo(nodePos[j]);
      if (distBetween > distanceLimit) continue; // don't connect if it's too far away
      hitInfo circleListCheck = rayCircleListIntersect(centers, radii, numObstacles, nodePos[i], dir, distBetween);
      if (!circleListCheck.hit) {
        neighbors[i].add(j);
      }
    }
  }
}

ArrayList<Integer> planPath(Vec2 startPos, Vec2 goalPos, Vec2[] centers, float[] radii, int numObstacles, Vec2[] nodePos, int numNodes) {
  ArrayList<Integer> path = new ArrayList();
  
  // Checking if there are any obstacles between start and goal
  Vec2 dir = goalPos.minus(startPos).normalized();
  float distBetween = startPos.distanceTo(goalPos);
  hitInfo circleListCheck = rayCircleListIntersect(centers, radii, numObstacles, startPos, dir, distBetween);
  
  // if there is, run the UCS algorithm, otherwise, simply return the direct path between the two
  if (circleListCheck.hit) path = runUCS(nodePos, numNodes, startPos, goalPos, centers, radii, numObstacles);

  return path;
}

// Helper function for UCS to check which node has the lowest cost/distance associated with it
int findMinDistNode(ArrayList<Integer> fringe) {
  int minDistID = -1;
  float minDist = MAX_FLOAT;
  for (int i : fringe) {
    float dist = distances[i];
    if (dist < minDist) {
      minDistID = i;
      minDist = dist;
    }
  }
  return minDistID;
}


// UCS/Dijkstra's Algorithm to find (hopefully) the shortest path on the graph
ArrayList<Integer> runUCS(Vec2[] nodePos, int numNodes, Vec2 startPos, Vec2 goalPos, Vec2[] centers, float[] radii, int numObstacles) {
  ArrayList<Integer> fringe = new ArrayList();  //New empty fringe
  ArrayList<Integer> path = new ArrayList(); //Path
  for (int i = 0; i < numNodes; i++) { //Clear visit tags and parent pointers
    visited[i] = false;
    parent[i] = -1; //No parent yet
    distances[i] = MAX_FLOAT;
  }

  //println("\nBeginning Search");
  
  // Getting all of the nodes that are visible from start
  for (int i = 0; i < numNodes; i++) {
      Vec2 dir = startPos.minus(nodePos[i]).normalized();
      float distBetween = nodePos[i].distanceTo(startPos);
      hitInfo circleListCheck = rayCircleListIntersect(centers, radii, numObstacles, nodePos[i], dir, distBetween);
      if (!circleListCheck.hit) {
        visited[i] = true;
        distances[i] = distBetween;
        fringe.add(i);
      }
  }

  int goalID = -1; // A variable to keep track of the closest node to graph
  ArrayList<Integer> seesGoal = new ArrayList<Integer>(); // list of all nodes that see the goal

  //println("Adding node", startID, "(start) to the fringe.");
  //println(" Current Fringe: ", fringe);
  
  while (fringe.size() > 0) {
    // Find the node with shortest distance in the fringe and remove it
    int currentNode = findMinDistNode(fringe);
    fringe.remove(Integer.valueOf(currentNode));
    
    // Check if goal is visible from this node
    Vec2 dir = goalPos.minus(nodePos[currentNode]).normalized();
    float distBetween = nodePos[currentNode].distanceTo(goalPos);
    hitInfo circleListCheck = rayCircleListIntersect(centers, radii, numObstacles, nodePos[currentNode], dir, distBetween);
    
    // If it is - it's a potential solution
    if (!circleListCheck.hit) {
      float goalDist = distances[currentNode] + distBetween;
      distances[currentNode] = goalDist;
      // Check if it's shorter than any other node closest to goal
      boolean foundSmaller = false;
      for (int i : seesGoal) {
        if (goalDist > distances[i]) {
          foundSmaller = true;
        }
      }
      if (foundSmaller) {
        seesGoal.add(currentNode);
      }
      else {
        goalID = currentNode;
        break;
      }
    }    
    
    // 
    for (int i = 0; i < neighbors[currentNode].size(); i++) {
      int neighborNode = neighbors[currentNode].get(i);
      float dist = distances[currentNode] + nodePos[i].distanceTo(nodePos[neighborNode]); 
      if (!visited[neighborNode] && dist < distances[neighborNode]) {
        visited[neighborNode] = true;
        distances[neighborNode] = dist;
        parent[neighborNode] = currentNode;
        fringe.add(neighborNode);
        //println("Added node", neighborNode, "to the fringe.");
        //println(" Current Fringe: ", fringe);
      }
    }
  }


  // If fringe is empty, we found no valid path
  if (fringe.size() == 0) {
    //println("No Path");
    path.add(0, -1);
    return path;
  }

  // Otherwise, we are reconstructing the path based on the parent[] array and returning the result
  //print("\nReverse path: ");
  int prevNode = parent[goalID];
  path.add(0, goalID);
  //print(goalID, " ");
  while (prevNode >= 0) {
    //print(prevNode," ");
    path.add(0, prevNode);
    prevNode = parent[prevNode];
  }
  //print("\n");

  return path;
}
