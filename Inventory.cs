using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    //public ArrayList KeysOnPlayerID = new ArrayList();
    //public int[] KeysOnPlayerID = new int[6];
    [HideInInspector]
    public DoorAction wantedKey;

    public static Inventory instance = null;

    public bool alwaysSelectPickedUpWeapon = true;
    [HideInInspector]
    public weaponselector weaponSelector;

    public List<GameObject> weaponPrefabs;

    // weapons
    public Weapon weaponTop = null;
    public Weapon weaponLeft = null;
    public Weapon weaponRight = null;
    public Weapon weaponBottom = null;
    private Weapon.Type selectedWeapon = Weapon.Type.KNIFE;


    // activatable items
    private int healthPotions = 0;


    // events & keys
    // public bool hasFinishedTutorial;
    public bool hasHQKey = false;



    void Awake() {
        if (instance != null) {
            Destroy(gameObject);
        } else {
            instance = this;
        }
    }


    void Start() {
        // weaponSelector = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<weaponselector>();
    }


    public void SwapWeapon(Weapon newWeapon, Transform newTransform) {
        Weapon oldWeapon = null;

        switch (newWeapon.type) {
            case Weapon.Type.KNIFE: oldWeapon = weaponTop; weaponTop = newWeapon; break;
            case Weapon.Type.RIFLE: oldWeapon = weaponRight; weaponRight = newWeapon; break;
            case Weapon.Type.THROWABLE: oldWeapon = weaponBottom; weaponBottom = newWeapon; break;
            case Weapon.Type.PISTOL: oldWeapon = weaponLeft; weaponLeft = newWeapon; break;
        }

        if (alwaysSelectPickedUpWeapon || selectedWeapon == newWeapon.type) {
            SelectWeapon(newWeapon.type);
        }

        if (oldWeapon != null) {
            Instantiate(weaponPrefabs[oldWeapon.prefabIndex], newTransform.position, newTransform.rotation);
        }
    }

    public void SelectWeapon(Weapon.Type type) {
        switch(type) {
            case Weapon.Type.KNIFE: SelectWeapon("KNIFE"); break;
            case Weapon.Type.RIFLE: SelectWeapon("RIFLE"); break;
            case Weapon.Type.THROWABLE: SelectWeapon("THROWABLE"); break;
            case Weapon.Type.PISTOL: SelectWeapon("PISTOL"); break;
        }
    }
    public void SelectWeapon(string type) {
        switch (type) {
            case "KNIFE": if (weaponTop != null) { weaponSelector.Switch(weaponTop.index); selectedWeapon = weaponTop.type; } break;
            case "RIFLE": if (weaponRight != null) { weaponSelector.Switch(weaponRight.index); selectedWeapon = weaponRight.type; } break;
            case "THROWABLE": if (weaponBottom != null) { weaponSelector.Switch(weaponBottom.index); selectedWeapon = weaponBottom.type; } break;
            case "PISTOL": if (weaponLeft != null) { weaponSelector.Switch(weaponLeft.index); selectedWeapon = weaponLeft.type; } break;
        }
    }


    public int getHealthPotions() { return healthPotions; }
    public bool hasHealthPotion() { return healthPotions > 0; }
    public void addHealthPotion(int amount = 1) {
        healthPotions += amount;
    }
    public void drinkHealthPotion() {
        healthPotions -= 1;
        // find player health and increase by health potion amount
    }
    /*public void AddKey(int keyID) {
        //KeysOnPlayerID
        KeysOnPlayerID.Add(keyID);
    }*/
}
