using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcessEtch : ProcessParent
{


    public override bool CallStep(int i)
    {
        bool toReturn = layerStackHold.etchLayer(layerStackHold.curMaterial, -i-1);
        layerStackHold.clearDeletes();
        if(!toReturn)
        {
            ErrorMessage = "No accessible material to etch!";
        }
        return toReturn;
    }

    public override void OnValueChanged(float newValue)
    {
        curStep = - (int) newValue;
        layerStackHold.sliceDeposits(curStep);
    }

    public override void UpdateSchematics() {

        schematicManager schematicManagerObject = GameObject.Find("schematicManager").GetComponent<schematicManager>();

        if (schematicManagerObject)
        {
            schematicManagerObject.updateSchem = true;
            schematicManagerObject.updateText("Etch");
        }
    }

}
