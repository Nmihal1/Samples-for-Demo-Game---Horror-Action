using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//[RequireComponent(typeof(Item))]
[RequireComponent(typeof(Activatable))]
public class PickupItemAction : MonoBehaviour
{
    private Activatable activatable;
    private WeaponObject weapon;
    private NoteItem note;
    private Key key;

    void Start() {
        activatable = GetComponent<Activatable>();
        weapon = GetComponent<WeaponObject>();
        note = GetComponent<NoteItem>();
        key = GetComponent<Key>();

        if (activatable != null) {
            if (weapon != null || note != null || key != null) {
                activatable.events.AddListener(PickUp);
                activatable.actionText = "Pick up " + (weapon != null ? weapon.displayName : key != null ? "Key" : "Note");
            }
        }
    }

    void PickUp() {
        // add to inventory
        WeaponObject w = GetComponent<WeaponObject>();
        if (w != null) {
            Inventory.instance.SwapWeapon(w.GetWeapon(), transform);
            GameObject.Destroy(gameObject);
        }

        NoteItem noteItem = GetComponent<NoteItem>();
        if (noteItem != null) {
            InventoryUIController.instance.PickedUpNote(noteItem.id);
            GameObject.Destroy(gameObject);
        }

        Key keyItem = GetComponent<Key>();
        if(keyItem != null) {
            keyItem.inv.UpdateSlots(keyItem.icon, keyItem.gameObject);
            keyItem.transform.parent = FindObjectOfType<Inventory>().transform;
            keyItem.gameObject.SetActive(false);
        }
    }

    public void MedkitPickup() { //used to pickup the medkit
        Inventory.instance.addHealthPotion(1);
        Destroy(this.gameObject);
    }

    public void LighterPickup() {
        GameObject.FindGameObjectWithTag("Player").GetComponent<playercontroller>().HasLighter = true;
        Destroy(this.gameObject);
    }

    public void GiveArmor(int ArmorValue) {
        playercontroller Script = GameObject.FindGameObjectWithTag("Player").GetComponent<playercontroller>();
        Script.Armor += ArmorValue;
        if(Script.Armor > Script.maxArmor) {
            Script.Armor = Script.maxArmor;
        }
        Destroy(this.gameObject);
    }

    public void BatteryPickup() {
        FlashlightController Script = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<FlashlightController>();
        Script.Battery = Script.MaxBattery;
        Script.light.intensity = 18f;
        Destroy(this.gameObject);
    }

    public void AmmoPickup(float ammo) {
        GameObject.FindGameObjectWithTag("Player").BroadcastMessage("pickAmmo", ammo);
        Destroy(this.gameObject);
    }

    public void UnlockButtonDoor(GameObject door) {
        door.GetComponent<DoorAction>().CurrentLockState = false;
    }

    public void Camouflage() {
        if (EnemyController.enemiesHuntingPlayer.Count > 0) {
            //cannot camouflage. being hunted by enemies
            Debug.Log("Player is being hunted. Cannot camouflage");
            return;
        }
        GameObject.FindGameObjectWithTag("Player").GetComponent<playercontroller>().tag = "Camouflaged";
        this.GetComponent<Activatable>().enabled = false;
    }

    // public void read(TextAsset file) {
    //     GameObject Panel = GameObject.Find("GUIelements").transform.GetChild(0).gameObject;
    //     string letter = file.ToString();
    //     Panel.GetComponentInChildren<Text>().text = letter;
    //     Panel.SetActive(true);
    // }
}
