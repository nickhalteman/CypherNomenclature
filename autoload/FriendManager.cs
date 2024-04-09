using Godot;
using System;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Net;
using ThreeDISevenZeroR.UnityGifDecoder;
using ThreeDISevenZeroR.UnityGifDecoder.Model;
using System.Runtime.InteropServices;

public class SteamFriendData
{
    public CSteamID steamID;
    public string personaName;
    public EPersonaState personaState;
    public EFriendRelationship relationship;
    public bool inGame;
    public FriendGameInfo_t gameInfo;
    public SpriteFrames avatar;
}

[Autoload(99)]
public static class FriendManager
{

    public static event Action<CSteamID> FriendUpdated;
	private static Callback<PersonaStateChange_t> m_PersonaStateChange;

    private static Queue<CSteamID> equippedProfileItemsQueue = new Queue<CSteamID>();


	private static void OnPersonaStateChange(PersonaStateChange_t pCallback)
	{
		CSteamID steamID = (CSteamID)pCallback.m_ulSteamID;

        SteamFriendData steamFriendData;

		if (!friendData.TryGetValue(steamID,out steamFriendData))
		{
            CacheFriend(steamID);

            if(!friendData.TryGetValue(steamID,out steamFriendData))
            {
                GD.PrintErr("Error: Couldn't get friend data for friend persona state change");
                return;
            }

		}

		if((pCallback.m_nChangeFlags & (EPersonaChange.k_EPersonaChangeName | EPersonaChange.k_EPersonaChangeNickname | EPersonaChange.k_EPersonaChangeNameFirstSet)) != 0)
		{
			string newPersonaName = SteamFriends.GetFriendPersonaName(steamID);
			GD.Print($"SteamFriends: {steamFriendData.personaName} changed name to {newPersonaName}.");
            steamFriendData.personaName = newPersonaName;
		}

        if ((pCallback.m_nChangeFlags & (EPersonaChange.k_EPersonaChangeStatus | EPersonaChange.k_EPersonaChangeComeOnline | EPersonaChange.k_EPersonaChangeGoneOffline)) != 0)
        {
            GD.Print($"SteamFriends: {steamFriendData.personaName} k_EPersonaChangeStatus");
            steamFriendData.personaState = SteamFriends.GetFriendPersonaState(steamID);
        }

        if ((pCallback.m_nChangeFlags & EPersonaChange.k_EPersonaChangeRelationshipChanged) != 0)
        {
            GD.Print($"SteamFriends: {steamFriendData.personaName} k_EPersonaChangeRelationshipChanged");
            steamFriendData.relationship = SteamFriends.GetFriendRelationship(steamID);
        }

        if ((pCallback.m_nChangeFlags & (EPersonaChange.k_EPersonaChangeGamePlayed | EPersonaChange.k_EPersonaChangeGameServer)) != 0)
        {
            GD.Print($"SteamFriends: {steamFriendData.personaName} game info changed");
            if (steamFriendData.inGame = SteamFriends.GetFriendGamePlayed(steamID, out steamFriendData.gameInfo))
            {
                if (steamFriendData.gameInfo.m_gameID == (CGameID)SteamUtils.GetAppID().m_AppId && steamFriendData.gameInfo.m_steamIDLobby.IsLobby())
                {
                    SteamMatchmaking.RequestLobbyData(steamFriendData.gameInfo.m_steamIDLobby);
                }
            }
        }

        if ((pCallback.m_nChangeFlags & EPersonaChange.k_EPersonaChangeAvatar) != 0)
		{
			GD.Print($"SteamFriends: {steamFriendData.personaName} avatar changed");
            LoadAvatarImage(steamID);
        }

        friendUpdates.Add(steamID);
	}


	private static Dictionary<CSteamID, SteamFriendData> friendData = new Dictionary<CSteamID, SteamFriendData>();

    private static HashSet<CSteamID> friendUpdates = new HashSet<CSteamID>();


	public static void CacheFriend(CSteamID friendID)
    {

        if (friendData.ContainsKey(friendID))
		{
            return;
        }

        SteamFriendData newFriendData = new SteamFriendData();
        newFriendData.steamID = friendID;

        newFriendData.personaName = SteamFriends.GetFriendPersonaName(friendID);

        GD.Print($"SteamFriends: Retrieving Data for Friend {newFriendData.personaName} [{friendID}] ");

        newFriendData.personaState = SteamFriends.GetFriendPersonaState(friendID);

        newFriendData.relationship = SteamFriends.GetFriendRelationship(friendID);
        newFriendData.inGame = SteamFriends.GetFriendGamePlayed(friendID, out newFriendData.gameInfo);
        if (newFriendData.inGame && newFriendData.gameInfo.m_gameID == (CGameID)SteamUtils.GetAppID().m_AppId && newFriendData.gameInfo.m_steamIDLobby.IsLobby())
        {
            SteamMatchmaking.RequestLobbyData(newFriendData.gameInfo.m_steamIDLobby);
        }


        friendData[friendID] = newFriendData;

        LoadAvatarImage(friendID);

        friendUpdates.Add(friendID);


    }

    public static bool TryGetFriendData(CSteamID friendID, out SteamFriendData steamFriendDataOut)
    {
        if (friendData.TryGetValue(friendID, out steamFriendDataOut))
        {
            return true;
        }

        if(SteamFriends.RequestUserInformation(friendID, false))
        {
            steamFriendDataOut = null;
            return false;
        } else
        {
            CacheFriend(friendID);
            return friendData.TryGetValue(friendID, out steamFriendDataOut);
        }
    }

	private static void LoadAvatarImage(CSteamID steamID)
    {
        try
        {
			if (!friendData.ContainsKey(steamID))
            {
                throw new Exception($"SteamFriends: Attempt to load avatar image for non-friend.");
            }

            //get animated profile if there is one
            equippedProfileItemsQueue.Enqueue(steamID);

            int avatarIndex = SteamFriends.GetLargeFriendAvatar(steamID);
            uint avatarWidth;
            uint avatarHeight;

            if (avatarIndex == -1)
            {
                //the avatar will come through OnAvatarImageLoaded
                return;
            }

            if (avatarIndex <= 0)
            {
                throw new Exception($"SteamFriends: Invalid large friend avatar index retrieved: {avatarIndex}");
            }

            if (!SteamUtils.GetImageSize(avatarIndex, out avatarWidth, out avatarHeight))
            {
                throw new Exception($"SteamFriends: Failed to retrieve avatar image size");
            }

            if (avatarHeight < 32 || avatarWidth < 32)
            {
                throw new Exception($"SteamFriends: Got invalid avatar image size: {avatarWidth} x {avatarHeight}");
            }


            //GD.Print($"SteamFriends: Creating buffer for avatar image...");
            byte[] avatarBytes = new byte[avatarWidth * avatarHeight * 4];

            if (!SteamUtils.GetImageRGBA(avatarIndex, avatarBytes, (int)(avatarWidth * avatarHeight * 4)))
            {
                throw new Exception($"SteamFriends: Failed to retrieve avatar image RGBA data.");
            }

            //GD.Print($"SteamFriends: Creating avatar image texture...");

            SpriteFrames avatarSpriteFrames = new SpriteFrames();
            avatarSpriteFrames.RemoveAnimation("default");
            avatarSpriteFrames.AddAnimation("default");
            avatarSpriteFrames.AddFrame(
                "default",
                ImageTexture.CreateFromImage(Image.CreateFromData((int)avatarWidth, (int)avatarHeight, false, Image.Format.Rgba8, avatarBytes))
                );

			friendData[steamID].avatar = avatarSpriteFrames;

        }
        catch (Exception e)
        {
            GD.Print($"SteamFriends: Error retrieving avatar for user [{steamID}]");
            GD.Print(e);
        }


    }

    private static CallResult<EquippedProfileItems_t> m_EquippedProfileItemsCallResult;
    private static void OnEquippedProfileItems(EquippedProfileItems_t pCallResult, bool bIOFailure)
    {

        if (bIOFailure || pCallResult.m_eResult != EResult.k_EResultOK)
		{
			GD.Print($"SteamFriends: Error retrieving profile items");
            GD.Print($"\tbIOFailure: {bIOFailure}");
            GD.Print($"\tm_eResult: {pCallResult.m_eResult}");
            GD.Print($"\tm_steamID: {pCallResult.m_steamID}");
            return;
        }

        if (pCallResult.m_bHasAnimatedAvatar)
		{
			string avatarURL = SteamFriends.GetProfileItemPropertyString(pCallResult.m_steamID, ECommunityProfileItemType.k_ECommunityProfileItemType_AnimatedAvatar, ECommunityProfileItemProperty.k_ECommunityProfileItemProperty_ImageSmall);

			if (avatarURL == null)
			{
                GD.Print($"Invalid Profile GIF Url for friend: {friendData[pCallResult.m_steamID].personaName}");
            }


            GD.Print($"Downloading Gif: {avatarURL}");

            HttpClientManager.DownloadStream(avatarURL, stream =>
            {

                GD.Print($"Started Decoding Gif for user: [{friendData[pCallResult.m_steamID].personaName}]");

                SpriteFrames spriteFrames = new SpriteFrames();

                spriteFrames.RemoveAnimation("default");

                spriteFrames.AddAnimation("default");

                spriteFrames.SetAnimationLoop("default", true);

                spriteFrames.SetAnimationSpeed("default", 1);

                GifStream gifStream = new GifStream(stream);

                while (gifStream.HasMoreData)
                {
                    if (gifStream.CurrentToken != GifStream.Token.Image)
                    {
                        gifStream.SkipToken();
                        continue;
                    }
                    GifImage frame = gifStream.ReadImage();
                    spriteFrames.AddFrame(
                        "default",
                        ImageTexture.CreateFromImage(Image.CreateFromData(gifStream.Header.width, gifStream.Header.height, false, Image.Format.Rgba8, MemoryMarshal.Cast<Color32, byte>(frame.colors.AsSpan()).ToArray())),
                        frame.SafeDelaySeconds
                    );
                }

                GD.Print($"Finished Decoding Gif for user: [{friendData[pCallResult.m_steamID].personaName}]");
                GD.Print($"\tFrames: {spriteFrames.GetFrameCount("default")}");

                friendData[pCallResult.m_steamID].avatar = spriteFrames;

                //Client.GameDataChanged();
                friendUpdates.Add(pCallResult.m_steamID);

            });

        }
    }


	private static Callback<AvatarImageLoaded_t> m_AvatarImageLoadedCallback;
	private static void OnAvatarImageLoaded(AvatarImageLoaded_t pCallback)
	{
		if (friendData.ContainsKey(pCallback.m_steamID))
		{
			LoadAvatarImage(pCallback.m_steamID);

            friendUpdates.Add(pCallback.m_steamID);
		}
	}


    public static void _Ready() {

		m_EquippedProfileItemsCallResult = CallResult<EquippedProfileItems_t>.Create(OnEquippedProfileItems);
		m_PersonaStateChange = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
		m_AvatarImageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);

		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		GD.Print($"SteamFriends: Found {friendCount} friends");

        CacheFriend(SteamUser.GetSteamID());

		for (int friendIndex = 0; friendIndex<friendCount; friendIndex++)
		{
			CSteamID steamID = SteamFriends.GetFriendByIndex(friendIndex, EFriendFlags.k_EFriendFlagImmediate);
			CacheFriend(steamID);
		}
    }

    public static void _Process(double delta)
    {
        foreach(CSteamID friendID in friendUpdates)
        {
            FriendUpdated?.Invoke(friendID);
        }
        friendUpdates.Clear();

        if(equippedProfileItemsQueue.Count > 0 && !m_EquippedProfileItemsCallResult.IsActive())
        {
            CSteamID steamID = equippedProfileItemsQueue.Dequeue();
            GD.Print($"SteamFriends: RequestEquippedProfileItems for: {friendData[steamID].personaName}");
            m_EquippedProfileItemsCallResult.Set(SteamFriends.RequestEquippedProfileItems(steamID));
        }

    }

}
