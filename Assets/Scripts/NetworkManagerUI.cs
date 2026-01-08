using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;


    [SerializeField] PlayerController testController;
    [SerializeField] Button giveOwnerShipButton;


    private void Awake()
    {

        SetUpConnectionButtons();
        giveOwnerShipButton.gameObject.SetActive(false);

        GameManager.Instance.OnNetWorkPostSpawned += (object sender, System.EventArgs e) =>
        {
            SetupOwnershipButton();
        };

    }
    private void OnDisable()
    {
        GameManager.Instance.OnNetWorkPostSpawned -= (object sender, System.EventArgs e) =>
        {
            SetupOwnershipButton();
        };
    }

    public void SetUpConnectionButtons()
    {
        hostButton.onClick.AddListener(() =>
        {
            Debug.Log("Host button clicked");
            Unity.Netcode.NetworkManager.Singleton.StartHost();
            Hide(hostButton.gameObject);
            Hide(clientButton.gameObject);

            giveOwnerShipButton.gameObject.SetActive(true);

        });
        clientButton.onClick.AddListener(() =>
        {
            Debug.Log("Client button clicked");
            Unity.Netcode.NetworkManager.Singleton.StartClient();
            Hide(clientButton.gameObject);
            Hide(hostButton.gameObject);
        });

    }

    public void SetupOwnershipButton()
    {
        // giveOwnerShipButton.onClick.AddListener(GiveOwnershipTo);

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null");
            return;
        }

        var isServer = NetworkManager.Singleton.IsServer;
        giveOwnerShipButton.gameObject.SetActive(isServer);

        giveOwnerShipButton.onClick.AddListener(() =>
        {
            Debug.Log("Give Ownership button clicked");
            GiveOwnershipToHost();
            Hide(giveOwnerShipButton.gameObject);

        });
    }

    public void GiveOwnershipTo()
    {
        // 서버에서만 소유권 변경 가능
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only server can change ownership.");
            return;
        }



        #region
        // 연결된 첫 번째 클라이언트에게 소유권을 넘김 (테스트용)
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                testController.GiveOwnershipTo(clientId);
                Debug.Log($"Ownership given to clientId: {clientId}");
                return;
            }
        }
        #endregion

        Debug.LogWarning("No remote client found to give ownership.");
    }


    private void Hide(GameObject paramObject)
    {
        // gameObject.SetActive(false);
        paramObject.SetActive(false);

    }
    public void GiveOwnershipToHost()
    {
        // 서버에서만 소유권 변경 가능
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only server can change ownership.");
            return;
        }

        // 호스트(서버)에게 소유권을 넘김
        ulong hostClientId = NetworkManager.Singleton.LocalClientId;
        testController.GiveOwnershipTo(hostClientId);
        Debug.Log($"Ownership given to host (clientId: {hostClientId})");
        Debug.Log("IsOwner: " + testController.IsOwner);
    }
}
