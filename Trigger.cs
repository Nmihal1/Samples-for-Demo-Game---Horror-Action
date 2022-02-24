using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Events;

public class Trigger : MonoBehaviour
{
    monologueSystem monologue;
    GameObject GMD;
    public bool Exit;
    public UnityEvent events = new UnityEvent();

    void Awake() {
        monologue = FindObjectOfType<monologueSystem>();
        GMD = GameObject.Find("GameMasterDisplay");
    }

    public void Elevator() {
        Transform plr = FindObjectOfType<playercontroller>().transform.parent;

        if(plr.parent == this.transform) {
            ElevatorController.Instance.PlayerDetect = false;
            plr.SetParent(null);
        } else {
            plr.SetParent(this.transform);
            ElevatorController.Instance.PlayerDetect = true;
            ElevatorController.Instance.StartCoroutine("GoToFloor", 0);
        }
    }

    public void Flicker(GameObject Light) {
        StartCoroutine("Delay", Light);
    }

    public void GMDText(string text) {
        GMD.GetComponent<Text>().text = text;
        GMD.SetActive(true);
    }
    public void MonologueText(string text) {
        monologue.TextToSay = text;
        monologue.gameObject.SetActive(true);
    }

    public void DoorClose(Animator Door) {
        Door.SetTrigger("close");
        this.gameObject.SetActive(false);
    }

    public void BackwardsCall(Animator stalker) {
        StartCoroutine("StalkerBackwards", stalker);
    }

    public void NoiseScare(AudioClip scareSound) {
        StartCoroutine("Noise", scareSound);
    }

    public void PassageUnlock(Collider Block) {
            Block.isTrigger = true;
    }

    public void lightningStrike() {
        Debug.Log("BAM");
    }

    public void Scream(float time) {
        StartCoroutine("ScreamEnum",time);
    }

    IEnumerator ScreamEnum(float time) {
        yield return new WaitForSeconds(time);
        GetComponent<ZombieJumpscare>().Audio();
    }

    IEnumerator Noise(AudioClip sound) {
        yield return new WaitUntil(new System.Func<bool>(() => !GetComponent<AudioSource>().isPlaying));
        GetComponent<AudioSource>().PlayOneShot(sound);
    }
 
    IEnumerator StalkerBackwards(Animator stalker) {
        stalker.SetTrigger("Retreat");
        yield return new WaitForSeconds(1.03f);
        Destroy(stalker.gameObject);
    }

    IEnumerator Delay(GameObject flashlight) {
        flashlight.GetComponent<LightFlickerEffect>().enabled = true;
        yield return new WaitForSeconds(1f);
        flashlight.GetComponent<LightFlickerEffect>().enabled = false;
    }

    void OnTriggerEnter(Collider other) {
        events.Invoke();
    }

    void OnTriggerExit(Collider other) {
        if(Exit) {
            events.Invoke();
        } else {
        this.gameObject.SetActive(false);
        }
    }
}
