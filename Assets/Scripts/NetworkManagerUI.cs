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
        hostButton.onClick.AddListener(() =>
        {
            Debug.Log("Host button clicked");
            Unity.Netcode.NetworkManager.Singleton.StartHost();
            Hide();

            giveOwnerShipButton.gameObject.SetActive(true);

        });
        clientButton.onClick.AddListener(() =>
        {
            Debug.Log("Client button clicked");
            Unity.Netcode.NetworkManager.Singleton.StartClient();
            Hide();
        });
        giveOwnerShipButton.onClick.AddListener(GiveOwnershipTo);
    }

    public void GiveOwnershipTo()
    {
        // 서버에서만 소유권 변경 가능
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only server can change ownership.");
            return;
        }

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

        Debug.LogWarning("No remote client found to give ownership.");
    }


    private void Hide()
    {
        gameObject.SetActive(false);

    }

}
