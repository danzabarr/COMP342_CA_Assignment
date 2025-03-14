using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class UIManager : MonoBehaviour
{

    public enum GameState
    {
        MainMenu,
        InGame,
        PauseMenu,
        DeathScreen
    }

    // References
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject deathScreenPanel;
    [SerializeField] private GameObject inGameOverlayPanel;

    private GameState currentState;


    public void Start()
    {
        mainMenuPanel.SetActive(true);
        pauseMenuPanel.SetActive(false);
        deathScreenPanel.SetActive(false);
        inGameOverlayPanel.SetActive(false);
        currentState = GameState.MainMenu;
    }
    

    // Main menu
    public void OnNewGamePressed()
    {
        mainMenuPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        deathScreenPanel.SetActive(false);
        inGameOverlayPanel.SetActive(true);
        currentState = GameState.InGame;
    }

    public void OnExitToDesktopPressed()
    {
        Application.Quit();
    }

    // In-game
    public void OnPausePressed()
    {
        pauseMenuPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        deathScreenPanel.SetActive(false);
        inGameOverlayPanel.SetActive(true);
        currentState = GameState.PauseMenu;
    }

    // Pause menu
    public void OnResumePressed()
    {
        pauseMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        deathScreenPanel.SetActive(false);
        inGameOverlayPanel.SetActive(true);
        currentState = GameState.InGame;
    }

    public void OnExitToMainMenuPressed()
    {
        mainMenuPanel.SetActive(true);
        pauseMenuPanel.SetActive(false);
        deathScreenPanel.SetActive(false);
        inGameOverlayPanel.SetActive(false);
        currentState = GameState.MainMenu;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch (currentState)
            {
                case GameState.MainMenu:
                    OnExitToDesktopPressed();
                    break;

                case GameState.InGame:
                    OnPausePressed();
                    break;

                case GameState.PauseMenu:   
                    OnResumePressed();
                    break;

                case GameState.DeathScreen:
                    OnExitToMainMenuPressed();
                    break;
            }
        }
    }
}
