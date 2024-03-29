using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicInterface : UserInterface
{
    public GameObject inventoryPrefab;

    public int xOffsetOfItemSlots;
    public int yOffsetOfItemSlots;
    public int amountOfColumns;

    public int xDisplayStart;
    public int yDisplayStart;

    public override void CreateSlots()
    {
        //Clear inventory, set it's size to the inventorySize, give each slot a parent
        inventoryToDisplay.ClearInventory();

        displayedItemsDictionary = new Dictionary<GameObject, InventorySlot>();
        //Create item slots in the empty inventory and add them to the InventorySlot dictionary
        for (int i = 0; i < inventoryToDisplay.inventory.inventorySlotArray.Length; i++)
        {
            var itemGameObject = Instantiate(inventoryPrefab, Vector3.zero, Quaternion.identity, transform);
            itemGameObject.GetComponent<RectTransform>().localPosition = GetItemSlotPosition(i);
            //Debug.Log("Created slot");
            displayedItemsDictionary.Add(itemGameObject, inventoryToDisplay.inventory.inventorySlotArray[i]);
            //Set display game object to slot
            inventoryToDisplay.inventory.inventorySlotArray[i].SetDisplayGameObject(itemGameObject);

            //Hook up each slot to unity events
            AddEvent(itemGameObject, EventTriggerType.PointerEnter, delegate { OnEnter(itemGameObject); });
            AddEvent(itemGameObject, EventTriggerType.PointerExit, delegate { OnExit(itemGameObject); });
            AddEvent(itemGameObject, EventTriggerType.BeginDrag, delegate { OnBeginDrag(itemGameObject); });
            AddEvent(itemGameObject, EventTriggerType.EndDrag, delegate { OnEndDrag(itemGameObject); });
            AddEvent(itemGameObject, EventTriggerType.Drag, delegate { OnDrag(itemGameObject); });
        }
    }
    public override void AssignDisplayGameObjectsToSlots()
    {
        foreach (KeyValuePair<GameObject, InventorySlot> pair in displayedItemsDictionary)
        {
            //Set display game object to slot
            pair.Value.SetDisplayGameObject(pair.Key);
        }
    }
    private Vector3 GetItemSlotPosition(int positionNumber)
    {
        return new Vector3(xDisplayStart + xOffsetOfItemSlots * (positionNumber % amountOfColumns), yDisplayStart + (-yOffsetOfItemSlots * (positionNumber / amountOfColumns)), 0f);
    }

}
