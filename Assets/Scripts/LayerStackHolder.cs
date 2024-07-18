using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.UIElements;
//
public class LayerStackHolder : MonoBehaviour
{

    public static int layerCount;

    //an array of all of the layers. each index is a list of material deposits at that height
    public List<GameObject>[] depLayers;

    //the current highest layer reached by anything in the design. Deposits will start one above here
    public int topLayer;

    //prefabs
    public GameObject meshGenPrefab;
    public GameObject layerStackPrefab;
    public GameObject processGenPrefab;
    public GameObject processEtchPrefab;
    public GameObject processWetEtchPrefab;
    public GameObject processIonEtchPrefab;
    public GameObject processCastPrefab;

    //constant, the height in pixels of a layer
    public float layerHeight;

    //the current material to be etched or deposited. Selected with dropdown
    public control.materialType curMaterial;

    //for deleting deposits
    public bool deletedFlag;
    public List<int> deletedLayers;

    public bool postDeleteCheckFlag = false;
    public bool wetEtch;

    schematicManager schematicManagerObject;


    // Start is called before the first frame update
    void Start()
    {
        layerCount = 100;
        depLayers = new List<GameObject>[layerCount].Select(item => new List<GameObject>()).ToArray();
        topLayer = -1;
        layerHeight = 0.1f;
        deletedLayers = new List<int>();
        deletedFlag = false;
        wetEtch = false;
        schematicManagerObject = GameObject.Find("schematicManager").GetComponent<schematicManager>();
    }


    public void onValueChange(int num) //Dropdown selection function
    {
        switch (num)
        {
            case 0:
                curMaterial = control.materialType.chromium;
                break;
            case 1:
                curMaterial = control.materialType.gold;
                break;
            case 2:
                curMaterial = control.materialType.aluminum;
                break;
            case 3:
                curMaterial = control.materialType.silicon;
                break;
            case 4:
                curMaterial = control.materialType.silicondioxide;
                break;
        }
    }

    //external functions called by buttons
    public void makePhotoResist()
    {
        depositLayer(control.materialType.photoresist, GameObject.Find("drawing_panel").GetComponent<paint>().grid);
        depositLayer(control.materialType.photoresistComplement, BitGrid.emptyIntersect(BitGrid.ones(), GameObject.Find("drawing_panel").GetComponent<paint>().grid));

        if (schematicManagerObject)
        {
            schematicManagerObject.updateText("Photoresist");
            schematicManagerObject.toolUsed(true);
        }
        //GameObject.Find("Control").GetComponent<control>().PhotoResistEdge.SetActive(true);
    }

    public void startDepositProcess()
    {
        Instantiate(processGenPrefab, transform.position, transform.rotation).gameObject.name = "New Process";

    }

    public void startEtchProcess()
    {
        if (wetEtch)
        {
            Instantiate(processWetEtchPrefab, transform.position, transform.rotation).gameObject.name = "New Process";
        }
        else
        {
            Instantiate(processEtchPrefab, transform.position, transform.rotation).gameObject.name = "New Process";
        }

    }

    public void startCastProcess() {
        Instantiate(processCastPrefab, transform.position, transform.rotation).gameObject.name = "New Process";
    }

    //deposits marked for deletion are only cleared at the end of each game step so they don't interfere with other functions
    void LateUpdate()
    {
        clearDeletes();

        if (schematicManagerObject.updateSchem)
        {
            schematicManagerObject.toolUsed(false);
        }
        schematicManagerObject.updateSchem = false;
    }

    public void clearDeletes()
    {
        if (deletedFlag)
        {
            bool topResetFlag = false;
            foreach (int i in deletedLayers)
            {
                List<GameObject> curList = depLayers[i];
                for (int j = 0; j < curList.Count; j++)
                {
                    if (!curList[j] || curList[j].GetComponent<meshGenerator>().toBeDestroyed)
                    {
                        curList.RemoveAt(j);
                    }
                }
                if (i == topLayer)
                {
                    topResetFlag = true;
                }

            }
            if (topResetFlag)
            {
                int i = topLayer;
                while (true)
                {
                    if (i < 0)
                    {
                        break;
                    }
                    if (depLayers[i].Count == 0)
                    {
                        topLayer--;
                        i--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            deletedFlag = false;
            deletedLayers.Clear();

        }
        if (postDeleteCheckFlag)
        {
            GameObject LRM = GameObject.Find("Level Requirement Manager");
            if (LRM)
            {
                LRM.GetComponent<levelRequirementManager>().checkRequirements();
            }
            postDeleteCheckFlag = false;
            GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().saveState();
        }
    }


    //inserts a deposit of a material into a particular layer
    bool addDeposit(int curlayer, BitGrid toDeposit, control.materialType layerMaterial, int newTimeOffset = 0)
    {
        if (curlayer >= layerCount)
        {
            return false;
        }
        if (curlayer > topLayer)
        {
            curlayer = topLayer + 1;
            topLayer++;
        }
        GameObject newMesh = Instantiate(meshGenPrefab, transform.position + new Vector3(0, layerHeight * (float)curlayer, 0), transform.rotation);
        newMesh.GetComponent<meshGenerator>().layerHeight = layerHeight;
        newMesh.GetComponent<meshGenerator>().grid.set(toDeposit);
        newMesh.GetComponent<meshGenerator>().initialize();
        newMesh.GetComponent<meshMaterial>().myMaterial = layerMaterial;
        newMesh.GetComponent<meshMaterial>().initialize(newTimeOffset);
        newMesh.transform.parent = gameObject.transform;
        depLayers[curlayer].Add(newMesh);

        return true;
    }

    //sets the BitGrid of a deposit to a new value, or destroys it if set to empty
    bool updateDeposit(BitGrid grid, GameObject deposit, int depLayer)
    {
        if (grid != null && !grid.isEmpty())
        {
            deposit.GetComponent<meshGenerator>().grid.set(grid);
            deposit.GetComponent<meshGenerator>().initialize();
        }
        else
        {
            deletedLayers.Add(depLayer);
            deletedFlag = true;
            deposit.GetComponent<meshGenerator>().toBeDestroyed = true;
            Destroy(deposit);
        }
        return grid != null && !grid.isEqual(deposit.GetComponent<meshGenerator>().grid);
    }

    //drops a 1 block layer of a material from the top, which cascades over any structures below
    public bool depositLayer(control.materialType layerMaterial, BitGrid inputGrid, int newTimeOffset = 0)
    {
        //start with the input grid at the top layer
        BitGrid grid = new BitGrid();
        //grid = the snow that's still falling
        grid.set(inputGrid);
        int curLayer = topLayer + 1;

        bool toReturn = false;

        //keep going down the layers until you hit the bottom
        while (curLayer > 0)
        {
            //in each layer get the BitGrid of everything in the layer below

            BitGrid thisDeposit = new BitGrid();
            thisDeposit.set(grid);

            //Find all of the surfaces that snow can fall on one layer below me, and union them together and put it in temp deposit(
            //start with an empty grid
            BitGrid tempDeposit = new BitGrid();
            tempDeposit.set(BitGrid.zeros());

            //go through each meshGenerator in the layer below, and add together all of their grids
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                tempDeposit.set(BitGrid.union(tempDeposit, curDeposit.GetComponent<meshGenerator>().grid));
            }
            //)

            //get all of the snow that's still falling, that can land on the surfaces we just collected, and store it in thisDeposit
            thisDeposit.set(BitGrid.intersect(tempDeposit, thisDeposit));

            //if there is any intersection between the falling snow and the surfaces, make some snow with that pattern
            if (!thisDeposit.isEmpty())
            {
                addDeposit(curLayer, thisDeposit, layerMaterial, newTimeOffset);
                toReturn = true;
            }


            //subtract the snow that just fell from the snow that's still falling
            grid.set(BitGrid.emptyIntersect(grid, thisDeposit));

            if (grid.isEmpty())
            {
                return toReturn;
            }
            curLayer--;
        }
        addDeposit(0, grid, layerMaterial, newTimeOffset);
        return true;
    }

    public bool depositGoldLayer(control.materialType layerMaterial, BitGrid inputGrid, int newTimeOffset = 0)
    {
        control.materialType gold = control.materialType.gold;
        control.materialType chromium = control.materialType.chromium;

        BitGrid grid = new BitGrid();
        //grid = the snow that's still falling
        grid.set(inputGrid);
        int curLayer = topLayer + 1;

        bool toReturn = false;

        while (curLayer > 0)
        {

            BitGrid thisDeposit = new BitGrid();
            thisDeposit.set(grid);

            BitGrid tempDeposit = BitGrid.zeros();

            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                control.materialType depositMaterialType = curDeposit.GetComponent<meshMaterial>().myMaterial;
                if (depositMaterialType == chromium || depositMaterialType == gold)
                {
                    tempDeposit.set(BitGrid.union(tempDeposit, curDeposit.GetComponent<meshGenerator>().grid));
                }
            }

            thisDeposit.set(BitGrid.intersect(tempDeposit, thisDeposit));

            BitGrid allDeposits = BitGrid.zeros();
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                control.materialType depositMaterialType = curDeposit.GetComponent<meshMaterial>().myMaterial;

                allDeposits = BitGrid.union(allDeposits, curDeposit.GetComponent<meshGenerator>().grid);

            }

            if (!thisDeposit.isEmpty())
            {
                addDeposit(curLayer, thisDeposit, layerMaterial, newTimeOffset); // this would be with mask 1
                toReturn = true;
            }

            grid.set(BitGrid.emptyIntersect(grid, allDeposits)); // mask 2 

            if (grid.isEmpty())
            {
                return toReturn;
            }

            curLayer--;
        }
        return toReturn;
    }
    
    // only want to keep the snow that fell last
    public bool depositWetLayer(control.materialType layerMaterial, BitGrid inputGrid, int newTimeOffset = 0)
    {
        //start with the input grid at the top layer
        BitGrid grid = new BitGrid();
        BitGrid lastFall = new BitGrid();
        //grid = the snow that's still falling
        grid.set(inputGrid);
        
        int curLayer = topLayer + 1;
        
        //keep going down the layers until you hit the bottom
        while (curLayer > 0)
        {
            //in each layer get the BitGrid of everything in the layer below
            BitGrid thisDeposit = new BitGrid();
            thisDeposit.set(grid);

            //Find all of the surfaces that snow can fall on one layer below me, and union them together and put it in temp deposit(
            //start with an empty grid
            BitGrid tempDeposit = new BitGrid();
            tempDeposit.set(BitGrid.zeros());

            //go through each meshGenerator in the layer below, and add together all of their grids
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                tempDeposit.set(BitGrid.union(tempDeposit, curDeposit.GetComponent<meshGenerator>().grid));
            }
            //)

            //get all of the snow that's still falling, that can land on the surfaces we just collected, and store it in thisDeposit
            thisDeposit.set(BitGrid.intersect(tempDeposit, thisDeposit));

            //if there is any intersection between the falling snow and the surfaces, make some snow with that pattern
            if (!thisDeposit.isEmpty())
            {
                lastFall.set(thisDeposit);
            }
            
            //subtract the snow that just fell from the snow that's still falling
            grid.set(BitGrid.emptyIntersect(grid, thisDeposit));

            if (grid.isEmpty())
            {
                addDeposit(curLayer, lastFall, layerMaterial, newTimeOffset);
                return lastFall.isEmpty();
            }
            curLayer--;
        }
        addDeposit(0, grid, layerMaterial, newTimeOffset);
        return true;
    }


    //removes the top-most layer of a particular material from the design
    public bool etchLayer(control.materialType etchMaterial, int newTimeOffset = 0)
    {
        Debug.Log("etch layer called");
        BitGrid grid = new BitGrid();
        grid.set(BitGrid.ones());
        int curLayer = topLayer + 1;

        bool toReturn = false;

        while (curLayer > 0)
        {
            BitGrid emptySpots = new BitGrid();
            BitGrid etchedSpots = new BitGrid();
            emptySpots.set(BitGrid.zeros());
            etchedSpots.set(BitGrid.zeros());
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                if (curDeposit.GetComponent<meshMaterial>().timeOffset >= 0 || (curDeposit.GetComponent<meshMaterial>().timeOffset < 0 && curDeposit.GetComponent<meshMaterial>().timeOffset >= newTimeOffset && curDeposit.GetComponent<meshMaterial>().myMaterial != etchMaterial))
                {
                    emptySpots.set(BitGrid.union(emptySpots, curDeposit.GetComponent<meshGenerator>().grid));
                }
            }
            bool anyFlag = false;
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                if (curDeposit.GetComponent<meshMaterial>().myMaterial == etchMaterial)
                {
                    if (curDeposit.GetComponent<meshMaterial>().timeOffset >= 0)
                    {
                        if (newTimeOffset < 0)
                        {
                            etchedSpots.set(BitGrid.union(BitGrid.intersect(curDeposit.GetComponent<meshGenerator>().grid, grid), etchedSpots));
                            if (!etchedSpots.isEmpty())
                            {
                               anyFlag = true;
                            }
                        }
                        if (updateDeposit(BitGrid.emptyIntersect(curDeposit.GetComponent<meshGenerator>().grid, grid), curDeposit, curLayer - 1))
                        {
                            toReturn = true;
                        }

                    }
                }
            }

            if (anyFlag)
            {
                addDeposit(curLayer - 1, etchedSpots, etchMaterial, newTimeOffset);
            }
            grid.set(BitGrid.emptyIntersect(grid, emptySpots));
            if (grid.isEmpty())
            {
                return toReturn;
            }
            curLayer--;
        }
        return toReturn;
    }


    //removes the material from exposed sides of a particular material from the design (for wet etch)
    public bool etchLayerAround(control.materialType etchMaterial, int newTimeOffset = 0)
    {
        Debug.Log("etch layer around called");

        BitGrid grid = new BitGrid();
        grid.set(BitGrid.ones());
        int curLayer = topLayer + 1;
        bool toReturn = false;
        while (curLayer > 0)
        {
            BitGrid emptySpots = new BitGrid();
            BitGrid etchedSpots = new BitGrid();
            emptySpots.set(BitGrid.zeros());
            etchedSpots.set(BitGrid.zeros());

            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                if (curDeposit.GetComponent<meshMaterial>().timeOffset >= 0)
                {
                    emptySpots.set(BitGrid.union(emptySpots, curDeposit.GetComponent<meshGenerator>().grid));
                }
            }

            BitGrid emptyContinuation = BitGrid.getIntersectedRegions(grid, BitGrid.invert(emptySpots));
            BitGrid emptyBorder = BitGrid.getBorderRegion(emptyContinuation);

            for (int i = 0; i < BitGrid.gridWidth; i++)
            {
                for (int j = 0; j < BitGrid.gridHeight; j++)
                {
                    if (i == 0 || j == 0 || i == BitGrid.gridWidth - 1 || j == BitGrid.gridHeight - 1)
                    {
                        emptyBorder.setPoint(i, j, 1);
                    }
                }
            }

            bool anyFlag = false;
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                meshMaterial curMeshMaterial = curDeposit.GetComponent<meshMaterial>();
                if (curMeshMaterial.myMaterial == etchMaterial)
                {
                    if (curMeshMaterial.timeOffset >= 0)
                    {
                        if (newTimeOffset < 0)
                        {
                            etchedSpots.set(BitGrid.union(BitGrid.intersect(curDeposit.GetComponent<meshGenerator>().grid, emptyBorder), etchedSpots));
                            anyFlag = true;
                        }
                        if(updateDeposit(BitGrid.emptyIntersect(curDeposit.GetComponent<meshGenerator>().grid, emptyBorder), curDeposit, curLayer - 1))
                        {
                            toReturn = true;
                        }

                    }
                }
            }

            if (anyFlag && !etchedSpots.isEmpty())
            {
                addDeposit(curLayer - 1, etchedSpots, etchMaterial, newTimeOffset);
            }

            grid.set(BitGrid.emptyIntersect(grid, emptySpots));

            for (int i = 0; i < BitGrid.gridWidth; i++)
            {
                for (int j = 0; j < BitGrid.gridHeight; j++)
                {
                    if (i == 0 || j == 0 || i == BitGrid.gridWidth - 1 || j == BitGrid.gridHeight - 1)
                    {
                        grid.setPoint(i, j, 1);
                    }
                }
            }

            curLayer--;
        }
        return toReturn;
    }


    public void clear()
    {
        for(int i = 0; i < 100; i++)
        {
            foreach(GameObject curLayer in depLayers[i])
            {
                Destroy(curLayer);
            }
            depLayers[i].Clear();
        }
        topLayer = -1;
    }

    //triggers a liftOff of the photomask, removing it and all deposits above it
    public void liftOff()
    {
        BitGrid grid = new BitGrid();
        grid.set(BitGrid.zeros()); ;
        for (int i = 0; i <= topLayer; i++)
        {
            foreach (GameObject curDeposit in depLayers[i])
            {
                if (curDeposit.GetComponent<meshMaterial>().myMaterial != control.materialType.photoresist)
                {
                    updateDeposit(BitGrid.emptyIntersect(curDeposit.GetComponent<meshGenerator>().grid, grid), curDeposit, i);
                }
            }
            foreach (GameObject curDeposit in depLayers[i])
            {
                if (curDeposit.GetComponent<meshMaterial>().myMaterial == control.materialType.photoresist)
                {
                    grid.set(BitGrid.union(grid, curDeposit.GetComponent<meshGenerator>().grid));
                    updateDeposit(BitGrid.zeros(), curDeposit, i);
                }
            }
        }
        //GameObject.Find("Level Requirement Manager").GetComponent<levelRequirementManager>().checkRequirements();
        postDeleteCheckFlag = true;
    }




    //when running a process, this function lets you select a particular time-step to show all the layers before. Called by the process slider
    public void sliceDeposits(int n)
    {
        int curLayer = 1;
        while (curLayer <= layerCount)
        {
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                meshMaterial mat = curDeposit.GetComponent<meshMaterial>();
                if (mat.timeOffset > 0)
                {
                    if (mat.timeOffset <= n)
                    {
                        curDeposit.SetActive(true);
                    }
                    else
                    {
                        curDeposit.SetActive(false);
                    }

                }
                else if (mat.timeOffset < 0)
                {
                    if (mat.timeOffset <= n)
                    {
                        curDeposit.SetActive(true);
                    }
                    else
                    {
                        curDeposit.SetActive(false);
                    }
                }

            }
            curLayer++;
        }
    }


    //when finishing a process, this function lets deletes all the deposits after the selected time step. Called when you click finish on a process
    public void cullDeposits(int n)
    {
        int curLayer = 1;
        while (curLayer <= layerCount)
        {
            foreach (GameObject curDeposit in depLayers[curLayer - 1])
            {
                curDeposit.SetActive(true);
                meshMaterial mat = curDeposit.GetComponent<meshMaterial>();
                if (mat.timeOffset > 0)
                {
                    if (mat.timeOffset < n)
                    {
                        curDeposit.GetComponent<meshMaterial>().initialize();
                    }
                    else
                    {
                        updateDeposit(BitGrid.zeros(), curDeposit, curLayer - 1);
                    }

                }
                else if (mat.timeOffset < 0)
                {
                    if (mat.timeOffset < n)
                    {
                        curDeposit.GetComponent<meshMaterial>().initialize();
                    }
                    else
                    {
                        updateDeposit(BitGrid.zeros(), curDeposit, curLayer - 1);
                    }
                }
            }
            curLayer++;

        }
    }


    /* Uses a depth-first-search approach to finding whether there is a connection between two cube spots on the wafer. 

    layer corresponds to the horizontal layer of the starting point, while pos.x * pos.y correspond to values in that plane.
    end.x corresponds to the horizontal layer of the ending point,
    while end.y & end.z correspond to the end.
    */
    bool connectionLoop(Vector3Int start, Vector3Int end, bool[,,] explored, bool[,,] conductive)
    {
        // similar approach to BitGrid.fill
        Stack<Vector3Int> posQueue = new Stack<Vector3Int>();
        conductive[end.x, end.y, end.z] = true;
        conductive[start.x, start.y, start.z] = true;
        posQueue.Push(new Vector3Int(start.x, start.y, start.z));

        int effectiveTop = topLayer + 1;
        if (effectiveTop > 99)
        {
            effectiveTop = 99;
        }

        while (posQueue.Count > 0)
        {
            Vector3Int curPos = posQueue.Pop();
            if ((curPos.x == end.x) && (curPos.y == end.y) && (curPos.z == end.z))
            {
                return true;
            }

            explored[curPos.x, curPos.y, curPos.z] = true;

            if (!conductive[curPos.x, curPos.y, curPos.z])
            {
                continue;
            }

            // move down
            if (curPos.x > 0 && !explored[curPos.x - 1, curPos.y, curPos.z])
            {
                explored[curPos.x - 1, curPos.y, curPos.z] = true;
                posQueue.Push(curPos + new Vector3Int(-1, 0, 0));
            }

            // move up
            if (curPos.x < effectiveTop && !explored[curPos.x + 1, curPos.y, curPos.z])
            {
                explored[curPos.x + 1, curPos.y, curPos.z] = true;
                posQueue.Push(curPos + new Vector3Int(1, 0, 0));
            }

            // move "north"
            if (curPos.z < 99 && !explored[curPos.x, curPos.y, curPos.z + 1])
            {
                explored[curPos.x, curPos.y, curPos.z + 1] = true;
                posQueue.Push(curPos + new Vector3Int(0, 0, 1));
            }

            // move "east"
            if (curPos.y < 99 && !explored[curPos.x, curPos.y + 1, curPos.z])
            {
                explored[curPos.x, curPos.y + 1, curPos.z] = true;
                posQueue.Push(curPos + new Vector3Int(0, 1, 0));
            }

            // move "south"
            if (curPos.z > 0 && !explored[curPos.x, curPos.y, curPos.z - 1])
            {
                explored[curPos.x, curPos.y, curPos.z - 1] = true;
                posQueue.Push(curPos + new Vector3Int(0, 0, -1));
            }

            // move "west"
            if (curPos.y > 0 && !explored[curPos.x, curPos.y - 1, curPos.z])
            {
                explored[curPos.x, curPos.y - 1, curPos.z] = true;
                posQueue.Push(curPos + new Vector3Int(0, -1, 0));
            }
        }

        // Never found a path from start to end with only conductive spots
        return false;
    }

    /* This code often crashes, most likely due to improper
    setup of the arrays due to numLayers miscalculation(?).
    - Ghost layers? */
    public bool getConnectionStatus(Vector3Int start, Vector3Int end)
    {
        int numLayers = topLayer + 1;
        //Debug.Log("numLayers: " + numLayers);

        if (topLayer < 0)
        {
            //Debug.Log("No layers");
            return false;
        }

        if (start.x > topLayer+1 || end.x > topLayer + 1)
        {
            return false;
        }

        int effectiveTop = numLayers + 1;
        if(effectiveTop > 100)
        {
            effectiveTop = 100;
        }

        bool[,,] explored = new bool[effectiveTop, 100, 100];
        bool[,,] conductive = new bool[effectiveTop, 100, 100];

        // set default to false
        for (int i = 0; i < effectiveTop; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                for (int k = 0; k < 100; k++)
                {
                    conductive[i, j, k] = false;
                    explored[i, j, k] = false;
                }
            }
        }
        for(int curLayer = 0; curLayer < effectiveTop; curLayer++)
        {
            List<GameObject> depLayer = depLayers[curLayer];
            for (int i = 0; i < depLayer.Count(); i++)
            {
                //check materials
                control.materialType mat = depLayer[i].GetComponent<meshMaterial>().myMaterial;
                if (mat != control.materialType.gold)
                {
                    continue;
                }

                BitGrid grid = depLayer[i].GetComponent<meshGenerator>().grid;
                for (int j = 0; j < 100; j++)
                {
                    for (int k = 0; k < 100; k++)
                    {
                        if (grid.getPoint(j, k) != 0)
                        {
                            conductive[curLayer, j, k] = true;
                        }
                    }
                }

            }
        }
        Debug.Log("Conductivity initialization complete.");
        return connectionLoop(start, end, explored, conductive);
    }

    /* Material to color index */
    public int matToColor(control.materialType mat) {
        switch (mat)
        {
            case control.materialType.chromium:
                return 1;
            case control.materialType.gold:
                return 2;
            case control.materialType.aluminum:
                return 3;
            case control.materialType.silicon:
                return 4;
            case control.materialType.silicondioxide:
                return 5;
            case control.materialType.photoresist:
                return 6;
            default: // air/empty
                return 0;
        }

    }

    public SchematicGrid crossSectionFromDepth(int depth)
    {
        if (depth < 0 || depth > 99)
            return SchematicGrid.zeros();

        if (topLayer < 0)
        {
            return SchematicGrid.zeros();
        }

        SchematicGrid crossSection = SchematicGrid.zeros();

        for (int curLayer = 0; curLayer <= topLayer; curLayer++)
        {
            /* All of the deposits that have material on that layer */
            List<GameObject> depLayer = depLayers[curLayer];
            for (int i = 0; i < depLayer.Count(); i++)
            {
                BitGrid grid = depLayer[i].GetComponent<meshGenerator>().grid;
                for (int j = 0; j < 100; j++)
                {
                    if (grid.getPoint(j, depth) != 0)
                    {
                        crossSection.setPoint(j, curLayer, matToColor(depLayer[i].GetComponent<meshMaterial>().myMaterial));
                    }
                }
            }
        }

        return crossSection;
    }
    
    public bool[,,] generateMold(int depth = 3)
    {

        int effectiveTop = topLayer + 2;
        if (effectiveTop > 100)
        {
            effectiveTop = 100;
        }

        /* mold contains every point that will have material */
        bool[,,] mold = new bool[effectiveTop, 100, 100];

        for (int i = 0; i < depth + topLayer; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                for (int k = 0; k < 100; k++)
                {
                    mold[i, j, k] = true;
                }
            }
        }

        for (int layer = 0; layer < effectiveTop; layer++)
        {
            List<GameObject> depLayer = depLayers[layer];
            for (int i = 0; i < depLayer.Count(); i++)
            {
                BitGrid grid = depLayer[i].GetComponent<meshGenerator>().grid;
                for (int j = 0; j < 100; j++)
                {
                    for (int k = 0; k < 100; k++)
                    {
                        if (grid.getPoint(j, k) != 0)
                        {
                            mold[layer, j, k] = false;
                        }
                    }
                }
            }

        }

        return mold;
    }

    public void flipMaterials() {
        int numLayers = topLayer + 1;
        //Debug.Log(numLayers);
        if (numLayers > 100)
        {
            numLayers = 100;
        }

        // swap each pair
        for (int pair = 0; pair <= numLayers / 2; pair++)
        {
            //Debug.Log(pair + " " + (numLayers - pair));

            // move amount
            int layersToMove = 2 * (numLayers / 2 - pair);

            // move top layer down
            List<GameObject> depLayer = depLayers[numLayers - pair - 1];

            for (int i = 0; i < depLayer.Count(); i++)
            {
                // move meshgenerator to correct location
                depLayer[i].GetComponent<meshGenerator>().transform.position -= new Vector3(0, layerHeight * layersToMove, 0);
            }

            // move bottom layer up
            depLayer = depLayers[pair];
            for (int i = 0; i < depLayer.Count(); i++)
            {
                // move meshgenerator to correct location
                depLayer[i].GetComponent<meshGenerator>().transform.position += new Vector3(0, layerHeight * layersToMove, 0);
            }


            // swap them
            depLayers[pair] = depLayers[numLayers - pair - 1];
            depLayers[numLayers - pair - 1] = depLayer;
        }


    }

    public void dropMaterials()
    {
        int effectiveTop = topLayer + 2;
        if (effectiveTop > 100)
        {
            effectiveTop = 100;
        }

        // find lowest point with material
        int bottom = 0;
        for (; bottom < effectiveTop; bottom++)
        {
            List<GameObject> depLayer = depLayers[bottom];
            if (depLayer.Count > 0)
                break;
        }
        Debug.Log(bottom);

        if (bottom == 0)
            return;
        
        // move down each layer
        for (int layer = bottom; layer < effectiveTop; layer++)
        {
            List<GameObject> depLayer = depLayers[layer];
            for (int i = 0; i < depLayer.Count(); i++)
            {
                GameObject mesh = depLayer[i];

                // move meshgenerator down

                mesh.GetComponent<meshGenerator>().transform.position -= new Vector3(0, layerHeight * bottom, 0);

                // place mesh in correct deplayer
                depLayers[layer - bottom].Add(mesh);
            }

            depLayer.Clear();
        }
        
    }

    /* Need to remove all non-PDMS material */
    public void startPeelProcess()
    {
        int effectiveTop = topLayer + 2;
        if (effectiveTop > 100)
        {
            effectiveTop = 100;
        }

        // remove non-PDMS material
        for (int layer = 0; layer < effectiveTop; layer++)
        {
            List<GameObject> depLayer = depLayers[layer];
            for (int i = 0; i < depLayer.Count(); i++)
            {
                if (depLayer[i].GetComponent<meshMaterial>().myMaterial != control.materialType.cast) {
                    updateDeposit(BitGrid.zeros(), depLayer[i], layer);
                }
            }
        }

        // flip PDMS cast
        //flipMaterials();

        clearDeletes();

        // move cast down
        dropMaterials();
    }

    public void startClearProcess()
    {
        clear();
    }

}

