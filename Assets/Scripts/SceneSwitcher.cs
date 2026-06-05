using System.Collections.Generic;
using Obi;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{

    SceneSwitcher instance;

    [SerializeField] List<string> scenes;

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }
    void Start()
    {
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.isPressed)
        {
            Application.Quit();
        }


        if (Keyboard.current.digit1Key.isPressed)
        {
            changeScene(scenes[0]);
        }
        if (Keyboard.current.digit2Key.isPressed)
        {
            changeScene(scenes[1]);
        }
        if (Keyboard.current.digit2Key.isPressed)
        {
            changeScene(scenes[2]);
        }
           
    }


    void changeScene(string scene)
    {

        SceneManager.LoadScene(scene);
    }
}
