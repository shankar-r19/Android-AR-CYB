using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayersManager : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
	public static PlayersManager Instance;

	public GameObject playerPrefab;

	// These things get auto populated at runtime
	public GameObject ObjectsMenu = null;
	public GameObject localPlayerHead = null;
	public ActivateTip localPlayerFingerPenTip = null; // set by the player's Debug_Keyboard script

	//public bool DEBUG_TEST_MESSAGE;

    private void Awake()
    {
		Debug.Assert(Instance == null, "Should not have multiple instances of this object!");
		Instance = this;

		DebugUI_NetworkStatusMenu.Instance.ShowStatusBad("NOT CONNECTED TO NETWORK");
	}

    // Start is called before the first frame update
    void Start()
    {
	}

    // Update is called once per frame
    void Update()
    {
		/**
        if (DEBUG_TEST_MESSAGE)
        {
			DebugUI_NetworkStatusMenu.Instance.ShowStatusBad("TEST MESSAGE BAD");
		}
		**/
    }

    public override void OnConnected()
    {
        base.OnConnected();

		DebugUI_NetworkStatusMenu.Instance.ShowStatusBad("Connected low level");
    }
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
		DebugUI_NetworkStatusMenu.Instance.ShowStatusBad("Connected to Master");
	}
    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
		DebugUI_NetworkStatusMenu.Instance.ShowStatusBad("Disconnected due to: " +cause.ToString());

		PhotonNetwork.ReconnectAndRejoin();
	}

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {

#if UNITY_EDITOR
		// PC deletes the players that left
		PhotonView pv = PhotonNetwork.GetPhotonView((int)otherPlayer.CustomProperties["PhotonViewID"]);

        if (pv == null) return;

		pv.RequestOwnership();
		StartCoroutine(SceneStepsManager.DestroyWhenOwned(pv));
#endif
	}

	/// <summary>
	/// When the local player joins a room, we need to get Photon to instantiate a player for everyone else, 
	/// and then we configure it so it can be properly locally controlled.
	/// </summary>
	public override void OnJoinedRoom()
    {
		base.OnJoinedRoom();
		DebugUI_NetworkStatusMenu.Instance.ShowStatusGood("Connected to Room");
		Debug.Log("PlayersManager: OnJoinedRoom() called by PUN. Now this client is in a room.");


		// First check what's going on in this room:
		// Are we the first ones ? If yes, we need to be the computer

#if UNITY_EDITOR
		// === PC ONLY ===
		// pc is always the master client; enable PC objects (serial port, etc)
		PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);

#else
		// we're not the PC
		if (PhotonNetwork.PlayerListOthers.Length == 0)
        {
			// means we're the only one here; in this case, just abort because this app isn't supposed to run like this
			DebugUI_NetworkStatusMenu.Instance.ShowStatusBad("ERROR: The PC running Unity needs to be started first before all other users. Please start that first, and restart this app.");
			return;
		}
		// if we got here we're all good
#endif







		// === SPAWN PLAYER ===
		// Spawn player and then configure local enable/disable scripts
		localPlayerHead = PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
		
		PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "PhotonViewID",localPlayerHead.GetPhotonView().ViewID} });
		
		// LOCAL CONTROL UI STUFF
		localPlayerHead.transform.Find("LocalControl").gameObject.SetActive(true);

		// HEAD and PENS		

		localPlayerHead.transform.Find("Head").GetComponent<FollowTheCamera>().enabled = (true);
		// hide head: localPlayerHead.transform.Find("Head/HeadModel").gameObject.SetActive(false);

		localPlayerHead.transform.Find("Pens").gameObject.GetComponent<FollowTheFinger>().enabled = (true);
		localPlayerHead.transform.Find("Pens").gameObject.GetComponent<FingerTrackingIndicator>().enabled = (true);

		// LASER POINTERS

		//localPlayerHead.transform.Find("FingerLaserPointers/smoothTip").gameObject.GetComponent<FollowTheFinger>().enabled = (true);
		localPlayerHead.transform.Find("FingerLaserPointers/controller").gameObject.SetActive(true);


		// HANDS
		if (localPlayerHead.transform.Find("HANDS") != null)
		{
			// control the hand
			if (localPlayerHead.transform.Find("HANDS/JointsController_L") != null)
				localPlayerHead.transform.Find("HANDS/JointsController_L").gameObject.SetActive(true);
			if (localPlayerHead.transform.Find("HANDS/JointsController_R") != null)
				localPlayerHead.transform.Find("HANDS/JointsController_R").gameObject.SetActive(true);

			// disable the model since the local player already has a hand
			localPlayerHead.transform.Find("HANDS/HandPivot_L").gameObject.SetActive(false);
			localPlayerHead.transform.Find("HANDS/HandPivot_R").gameObject.SetActive(false);
		}

		ExitGames.Client.Photon.Hashtable cp = PhotonNetwork.LocalPlayer.CustomProperties;
		if (cp == null) cp = new ExitGames.Client.Photon.Hashtable();
		cp["platform"] = Application.platform;
		PhotonNetwork.LocalPlayer.SetCustomProperties(cp);
	}

	public bool IsPlayerReady()
    {
		// Returns true if the local player has been initialized and photon is running

		return (PhotonNetwork.IsConnectedAndReady && localPlayerHead != null);
    }
}
