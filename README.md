**Dokumentáció**

# SMAASH – Algoritmus dokumentáció

## 1. A Fusion hálózati keretrendszer alapfogalmai

A SMAASH Unity kliense a **Photon Fusion 2** hálózati SDK-t használja a valós idejű multiplayer megvalósításához. Az alábbiakban azok az alapfogalmak szerepelnek, amelyek a kód megértéséhez szükségesek.

### `NetworkBehaviour`

A `NetworkBehaviour` hozzáférést ad a Fusion-specifikus életciklus-metódusokhoz és a hálózati tulajdonságokhoz.

```csharp
public class PlayerMovement : NetworkBehaviour { ... }
```

### `[Networked]` – hálózaton szinkronizált tulajdonságok

A `[Networked]` attribútummal jelölt property-k értékét a Fusion automatikusan replikálja minden csatlakozott kliensnek. Az értéket csak az **StateAuthority** (a szerver/host) írhatja, a kliensek csak olvashatják.

```csharp
// PlayerMovement.cs
[Networked] public bool IsFacingLeft { get; set; }
[Networked] public float NetworkSpeed { get; set; }
[Networked] public bool NetworkIsJumping { get; set; }
```

### `HasInputAuthority` és `HasStateAuthority`

A Fusion megkülönbözteti, hogy egy adott kliensnek van-e joga az objektum **inputját** küldeni, vagy az objektum **állapotát** írni:

- **`HasInputAuthority`**: az a kliens, aki a karaktert vezérli (a helyi játékos). Például csak ő látja a saját kameráját, ő kezeli az inputot.
- **`HasStateAuthority`**: általában a host/szerver. Ő írja a `[Networked]` property-ket, ő hajtja végre a fizikát és a sebzés logikát.

```csharp
// CameraController.cs – kamera csak a helyi játékosnál aktív
if (cam) cam.enabled = Object.HasInputAuthority;

// PlayerMovement.cs – animáció értékeket csak a szerver írja
if (Object.HasStateAuthority)
{
    NetworkSpeed = Mathf.Abs(rb.velocity.x);
    NetworkIsJumping = !IsGrounded();
}
```

### `Spawned()` – hálózati inicializáció

A `Spawned()` a `Start()`/`Awake()` hálózatos megfelelője: akkor hívódik, amikor a hálózati objektum létrejön és a Fusion már beállította az authority-ket. Emiatt itt biztonságos lekérdezni, hogy a kliens `HasInputAuthority`-e.

```csharp
// PlayerMovement.cs
public override void Spawned()
{
    rb = GetComponent<Rigidbody2D>();

    if (Object.HasInputAuthority && (jumpButtonOwner == null || jumpButtonOwner == this))
    {
        jumpButtonOwner = this;
        isJumpButtonOwner = true;
    }

    extraJumps = maxAirJumps;
    SetupJumpButton();
}
```

### `FixedUpdateNetwork()` – determinisztikus hálózati tick

A Fusion nem a Unity `Update()`-jét, hanem a `FixedUpdateNetwork()`-öt használja a játéklogika futtatásához. Ez minden hálózati tick-ben fut (alapértelmezetten 30/s vagy 60/mp), és **determinisztikus** – azaz minden kliensen pontosan ugyanabban a sorrendben hajtódik végre, ami megelőzi a szinkronizációs problémákat.

```csharp
// PlayerMovement.cs
public override void FixedUpdateNetwork()
{
    if (isCountingDown) return;

    PlayerHealth playerHealth = GetComponent<PlayerHealth>();
    if (playerHealth != null && playerHealth.isDead)
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        UpdateNetworkedAnimationValues();
        return;
    }

    if (GetInput(out NetworkInputData data))
    {
        rb.velocity = new Vector2(data.moveInput.x * speed, rb.velocity.y);
    }
    // ...
}
```

A `GetInput()` metódus csak egy azon a kliensen ad vissza adatot, ahol input történt – a többi kliensen üres struktúrát ad vissza.

### `Render()` – vizuális frissítés

A `Render()` minden képkockában fut (ellentétben a `FixedUpdateNetwork()`-kel), és kizárólag vizuális frissítésre való. Mivel a `[Networked]` értékek már szinkronban vannak, itt biztonságosan olvashatók az animáció értékeinek a beállításához.

```csharp
// PlayerMovement.cs
public override void Render()
{
    if (animator)
    {
        animator.SetFloat("speed", NetworkSpeed);
        animator.SetBool("isJumping", NetworkIsJumping);
    }
    spriteRenderer.flipX = IsFacingLeft;
}
```

### RPC – Remote Procedure Call

Az RPC (Remote Procedure Call) olyan metódus, amelyet az egyik kliensen hívnak meg, de egy **másik kliensen** (vagy a szerveren) fut le. A Fusion RPC-k az `[Rpc]` attribútummal jelöltek, és meg kell adni, hogy honnan érkezik a hívás (`RpcSources`) és hova szól (`RpcTargets`).

A SMAASH-ban három fő RPC irányt használ a kód:

| Forrás → Cél | Mikor használják |
|---|---|
| `InputAuthority → StateAuthority` | A játékos kliens kér valamit a szervertől (pl. sebzés, lövés) |
| `StateAuthority → All` | A szerver eredményt küld minden kliensnek (pl. halál animáció) |
| `All → StateAuthority` | Bárki küldhet kérést a szervernek |

```csharp
// PlayerHealth.cs – a kliens sebzést kér a szervertől
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
private void RPC_RequestDamage(int damage)
{
    if (isDead) return;
    CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
    if (CurrentHealth <= 0)
    {
        isDead = true;
        RPC_BroadcastDeath(Object.InputAuthority.PlayerId);
    }
}

// A szerver értesít mindenkit a halálról
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_BroadcastDeath(int deadPlayerId)
{
    if (animator) animator.SetBool("isDead", true);
    if (meleeAttack) meleeAttack.enabled = false;
    if (playerMovement) playerMovement.enabled = false;
    // ...
    NetworkHandler.Instance.HandleMatchEnded(deadPlayerId);
}
```

---

## 2. Játékos mozgás és input kezelés

### 2.1 `NetworkInputData` (struktúra)

Az összes hálózaton átküldendő inputot tároló struktúra. Az `INetworkInput` interfész implementálása szükséges ahhoz, hogy a Fusion automatikusan kezelje a szállítását.

```csharp
public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public bool jumpPressed;
}
```

### 2.2 `LocalInputHandler` – input összegyűjtése

A `GetNetworkInput()` metódus billentyűzet és mobil joystick inputokat olvas, és egységes struktúrában adja vissza. A `NetworkHandler.OnInput()` hívja minden tick-ben.

```csharp
public NetworkInputData GetNetworkInput()
{
    NetworkInputData data = new NetworkInputData();

    Vector2 keyboardInput = Vector2.zero;
    if (Keyboard.current != null)
    {
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            keyboardInput.x = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            keyboardInput.x = 1;
    }

    Vector2 joystickInput = Vector2.zero;
    if (joystick != null)
        joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);

    // Billentyűzet prioritása van a joystick felett
    data.moveInput = keyboardInput.magnitude > 0.1f ? keyboardInput : joystickInput;

    bool keyboardJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    data.jumpPressed = keyboardJump || jumpButtonPressed;

    return data;
}
```

A `NetworkHandler` az `OnInput` callbackben hívja ezt a metódust, és a Fusion-nak adja át:

```csharp
// NetworkHandler.cs
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new NetworkInputData();
    if (runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObj))
    {
        var handler = playerObj.GetComponent<LocalInputHandler>();
        if (handler != null)
            data = handler.GetNetworkInput();
    }
    input.Set(data);
}
```

### 2.3 `PlayerMovement` – mozgás és ugrás

A `FixedUpdateNetwork()`-ben a `GetInput()` kinyeri a kliens inputját, és a Rigidbody sebességét frissíti:

```csharp
public override void FixedUpdateNetwork()
{
    if (isCountingDown) return;

    PlayerHealth playerHealth = GetComponent<PlayerHealth>();
    if (playerHealth != null && playerHealth.isDead)
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        UpdateNetworkedAnimationValues();
        return;
    }

    if (GetInput(out NetworkInputData data))
        rb.velocity = new Vector2(data.moveInput.x * speed, rb.velocity.y);

    if (jumpRequestedFromButton)
    {
        jumpRequestedFromButton = false;
        Jump();
    }

    UpdateNetworkedAnimationValues();
}
```

Az ugrás logika megkülönbözteti a talajról és a levegőből történő ugrást (kettős ugrás implementációhoz):

```csharp
void Jump()
{
    if (IsGrounded())
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
        extraJumps = maxAirJumps;
        return;
    }

    if (extraJumps > 0)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
        extraJumps--;
    }
}

// Talajérzékelés: kis sugarú körrel ellenőrzi a groundLayer-t
bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
```

Az animációs értékeket a szerver frissíti (`HasStateAuthority`), de a `Render()` minden kliensen alkalmazza azokat:

```csharp
void UpdateNetworkedAnimationValues()
{
    if (Object.HasStateAuthority)
    {
        NetworkSpeed = Mathf.Abs(rb.velocity.x);
        NetworkIsJumping = !IsGrounded();
        if (rb.velocity.x > 0.1f)       IsFacingLeft = false;
        else if (rb.velocity.x < -0.1f) IsFacingLeft = true;
    }
}

public override void Render()
{
    if (animator)
    {
        animator.SetFloat("speed", NetworkSpeed);
        animator.SetBool("isJumping", NetworkIsJumping);
    }
    spriteRenderer.flipX = IsFacingLeft;
}
```

---

## 3. Harci rendszer

### 3.1 `MeleeAttack` – Közelharci támadás

A közelharci támadás RPC-láncon keresztül működik: a kliens kér → szerver ellenőriz és sebez → szerver értesít mindenkit az animációról.

```csharp
private void OnAttackInput(InputAction.CallbackContext context)
{
    if (!canAttack) return;
    RPC_PerformAttack(spriteRenderer.flipX);
    StartCoroutine(AttackCooldown());
}

[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
private void RPC_PerformAttack(bool isFacingLeft)
{
    Transform activePoint = isFacingLeft ? attackPointOpposite : attackPoint;

    //Egy kört rajzol ki, ahol keresi az enemyLayerrel rendelkező objektumokat és vissza adja azt a változóba
    Collider2D hitEnemy = Physics2D.OverlapCircle(activePoint.position, attackRange, enemyLayer);

    if (hitEnemy != null)
    {
        if (hitEnemy.TryGetComponent<PlayerHealth>(out var health))
            health.TakeDamageCaller(damage);
    }

    RPC_BroadcastAttack(); // animáció minden kliensen
}

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_BroadcastAttack()
{
    StartCoroutine(PlayAttackAnimation());
}
```

### 3.2 `ShootingAttack` – Lőfegyver támadás

A lövedék hálózati spawnjához a kliens RPC-t küld a szervernek, amely létrehozza az objektumot a Fusion `Runner.Spawn()` metódusával:

```csharp
private void OnAttackInput(InputAction.CallbackContext context)
{
    if (!canAttack) return;
    // A két kiindulási pont (jobbra vagy balra néz a karakter) közül kiválasztjuk azt, amelyik a megfelelő
    Transform activePoint = spriteRenderer.flipX ? attackPointOpposite : attackPoint;
    SpawnBulletRpc(activePoint.position, activePoint.rotation, spriteRenderer.flipX);
    StartCoroutine(AttackCooldown());
}

[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void SpawnBulletRpc(Vector3 position, Quaternion rotation, bool facingLeft)
{
    if (Runner != null && bulletPrefab.IsValid)
    {
        // Runner.Spawn: hálózati objektumot hoz létre, minden kliensnek replikálva
        NetworkObject bulletNetObj = Runner.Spawn(bulletPrefab, position, rotation);
        Bullet bullet = bulletNetObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            Vector2 fireDirection = facingLeft ? Vector2.left : Vector2.right;
            bullet.SetDirection(fireDirection);
        }
    }
}
```

### 3.3 `Bullet` – Lövedék

A `Bullet` is `NetworkBehaviour`, így mozgása szinkronizált. Ütközéskor hálózati despawn történik:

```csharp
public override void FixedUpdateNetwork()
{
    if (rb != null)
        rb.velocity = direction * speed;
}

public void SetDirection(Vector2 newDirection)
{        
    direction = newDirection.normalized;
        
    // Elforgatja a lövedéket a jó irányba
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
}

void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.TryGetComponent<PlayerHealth>(out var health))
    {
        health.TakeDamageCaller(damage);

        // Runner.Despawn: hálózaton minden kliensről eltávolítja az objektumot
        if (Runner != null)
            Runner.Despawn(Object);
        else
            Destroy(gameObject);
    }
}
```

---

## 4. Életerő és halál rendszer

### 4.1 `PlayerHealth`

Az életerő egy `[Networked]` property, amelynek változásakor automatikusan fut a UI frissítés:

```csharp
[Networked, OnChangedRender(nameof(OnHealthChanged))]
private int CurrentHealth { get; set; }

[Networked] public bool isDead { get; set; }
```

A `Spawned()`-ban a játékos PlayerId-je alapján dől el, melyik sarokba rakja a játékos életerejét:

```csharp
public override void Spawned()
{
    if (UIManager.Instance != null)
    {
        // PlayerId páratlan → bal felső sarok, páros → jobb felső sarok
        if (Object.InputAuthority.PlayerId % 2 != 0)
            myUIBar = UIManager.Instance.healthBar1;
        else
            myUIBar = UIManager.Instance.healthBar2;
    }

    if (Object.HasStateAuthority)
    {
        CurrentHealth = maxHealth;
        isDead = false;
    }
    UpdateVisuals();
}
```

A sebzés kétlépéses RPC-n keresztül megy:

```csharp
// 1. lépés: a sérülést elszenvedő karakter bármely kliensről kérhet sebzést (ezt hívjuk meg a támadás scriptekből), paraméterként bekérjük a támadás által okozozz sebzés mértékét
public void TakeDamageCaller(int damage)
{
    if (isDead) return;
    RPC_RequestDamage(damage);
}

// 2. lépés: a szerver érvényesíti és végrehajtja
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
private void RPC_RequestDamage(int damage)
{
    if (isDead) return;
    CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

    if (CurrentHealth <= 0)
    {
        isDead = true;
        RPC_BroadcastDeath(Object.InputAuthority.PlayerId);
    }
}

// 3. lépés: halál esemény szétküldése minden kliensnek, leáll a mozgás mindkét játékosnál és a NetworkHandle scriptből meghivja a HandleMatchEnded függvényt, ami a halott játékos Id-ját kéri be paraméterként
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_BroadcastDeath(int deadPlayerId)
{
    if (animator) animator.SetBool("isDead", true);
    if (meleeAttack) meleeAttack.enabled = false;
    if (playerMovement) playerMovement.enabled = false;

    var rb = GetComponent<Rigidbody2D>();
    if (rb) rb.constraints = RigidbodyConstraints2D.FreezeAll;

    NetworkHandler.Instance.HandleMatchEnded(deadPlayerId);
}
```

---

## 5. Hálózati kommunikáció és meccs kezelés

### 5.1 `NetworkHandler` – csatlakozás és szoba kezelés

A játék indításakor a `NetworkHandler` létrehoz egy `NetworkRunner` objektumot, és meghívja a Fusion `StartGame()` metódusát:

```csharp
// Szoba létrehozása vagy szobához csatlakozás után fut le
public void RoomCreateAndJoin()
{
    if (_pendingCharacterSelectMode == GameMode.Single)
    {
        StartGame(GameMode.Single, "LocalTestRoom");
        return;
    }
        
    SceneManager.LoadScene(_waitingRoomSceneName);
        
    // Use a coroutine to create/join room after scene loads
    StartCoroutine(CreateOrJoinRoomAfterSceneLoad());
}

private IEnumerator CreateOrJoinRoomAfterSceneLoad()
{
    // Wait for the scene to load
    yield return null;
    yield return null;
        
     string roomName = string.IsNullOrWhiteSpace(_pendingRoomName) ? "DefaultRoom" : _pendingRoomName;

    if (_pendingCharacterSelectMode == GameMode.Host)
    {
        Debug.Log("[NetworkHandler] Creating room: " + roomName);
        StartGame(GameMode.Host, roomName);        
    }
    else
    {
        Debug.Log("[NetworkHandler] Joining room: " + roomName);
        StartGame(GameMode.Client, roomName);
    }
}


async void StartGame(GameMode mode, string roomName)
{
    if (_isConnecting || _isCancellingMatchmaking || _isDisposing) return;
    _isConnecting = true;

    // NetworkRunner: a Fusion kapcsolat motorja, alapértelmezetten szerepel a Fusion-ben – egy DontDestroyOnLoad objektumon él, tehát a jelenetek váltása alatt továbbra is fut ez a script
    GameObject runnerObj = new GameObject("NetworkRunner");
    DontDestroyOnLoad(runnerObj);

    _runner = runnerObj.AddComponent<NetworkRunner>();
    _runner.ProvideInput = true;   // ez a kliens küld inputot
    _runner.AddCallbacks(this);    // a NetworkHandler kapja a Fusion callbackeket
    runnerObj.AddComponent<NetworkSceneManagerDefault>();

    await _runner.StartGame(new StartGameArgs
    {
        GameMode = mode,           // Host, Client vagy Single (tesztelésre)
        SessionName = _lastRoomName,
        PlayerCount = mode == GameMode.Single ? 1 : 2,
        SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
    });
}
```

Amikor egy játékos csatlakozik (`OnPlayerJoined`), a host elmenti a karakterválasztást és ellenőrzi, megvan-e már mindenki:

```csharp
public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    int playerCount = runner.ActivePlayers.Count();
    UpdateWaitingRoomStatus(playerCount);

    if (player == runner.LocalPlayer)
    {
        int mySelection = PlayerPrefs.GetInt("selectedOption", 0);
        if (runner.IsServer)
        {
            _playerSelections.Add(player, mySelection);
            CheckStartCondition(runner);
        }
        else
        {
            // Kliens reliable adatként küldi a karakterválasztást a hostnak
            runner.SendReliableDataToServer(default, BitConverter.GetBytes(mySelection));
        }
    }
}

private void CheckStartCondition(NetworkRunner runner)
{
    if (!runner.IsServer || _sceneLoadRequested) return;
    if (runner.ActivePlayers.Count() >= 2 && _playerSelections.Count >= 2)
    {
        _sceneLoadRequested = true;
        runner.LoadScene(_gameSceneName);  // Fusion-on keresztül tölti be a jelenetet mindenkinél
    }
}
```

Jelenet betöltése után a szerver spawnolja a karaktereket a megfelelő spawn pontokra:

```csharp
public void OnSceneLoadDone(NetworkRunner runner)
{
    if (!runner.IsServer) return;

    foreach (var player in runner.ActivePlayers)
    {
        if (_spawnedCharacters.ContainsKey(player)) continue;

        int index = _playerSelections.TryGetValue(player, out int sel) ? sel : 0;
        Character characterData = characterDatabase.GetCharacter(index);

        bool isLeftSide = (player == runner.LocalPlayer);
        string spawnPointName = isLeftSide ? player1SpawnPointName : player2SpawnPointName;
        GameObject spawnPointObj = GameObject.Find(spawnPointName);

        Vector3 spawnPos = spawnPointObj.transform.position;

        // runner.Spawn: hálózati prefabot hoz létre, az adott PlayerRef ownership-jével
        NetworkObject obj = runner.Spawn(
            characterData.playerPrefab, spawnPos, spawnPointObj.transform.rotation, player);

        // SetPlayerObject: összeköti a PlayerRef-et a hálózati objektummal
        // – ez szükséges ahhoz, hogy az OnInput callback tudja, ki küldte az inputot
        runner.SetPlayerObject(player, obj);
        _spawnedCharacters.Add(player, obj);
    }
}
```

### 5.2 Meccs eredmény beküldése a backendnek

Játékos halála után a `NetworkHandler` összegyűjti az adatokat és elküldi a backend API-nak:

```csharp
private IEnumerator PostMatchResultAndReturnToLobby(int deadPlayerId)
{
    string endedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");
    int localPhotonPlayerId = _runner != null ? _runner.LocalPlayer.PlayerId : -1;
    string localResult = localPhotonPlayerId == deadPlayerId ? "lose" : "win";
    string networkStatus = _lastGameMode == GameMode.Single ? "offline" : "online";

    var payload = new MatchResultDto
    {
        session_id = ResolveSessionId(),
        started_at = _matchStartedAt,
        ended_at = endedAt,
        level_id = levelId,
        participation = new MatchParticipationDto
        {
            player_id = PlayerPrefs.GetInt("selected_profile_id", -1),
            character_id = ResolveLocalCharacterId(),
            result = localResult,
            network_status = networkStatus
        }
    };

    // AuthClient.PostAuthorizedJson elvégzi a tényleges HTTP POST kérést
    // matchResultEndpoint: a végpont ahova a POST megy
    yield return StartCoroutine(authClient.PostAuthorizedJson(
        matchResultEndpoint, payload, (ok, body) =>
    {
        if (!ok) Debug.LogWarning($"Match result post failed: {body}");
        else     Debug.Log($"Match result posted: {body}");
    }));

    CancelMatchmaking();
}
```

A session azonosító generálása: a Fusion szoba nevéből determinisztikus GUID jön létre MD5 hash segítségével, hogy az adatbázis ugyanazt az azonosítót kapja minden klienstől:

```csharp
private static string ToDeterministicGuid(string value)
{
    if (Guid.TryParse(value, out var parsed))
        return parsed.ToString();

    string normalized = string.IsNullOrWhiteSpace(value) ? "smaash-session" : value.Trim();

    using var md5 = MD5.Create();
    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(normalized));
    return new Guid(hash).ToString();
}
```

---

## 6. Kommunikáció a külső backenddel

Az `AuthClient` osztály felelős minden, a SMAASH webes backenddel folytatott kommunikációért. A backend URL konfigurálható: fejlesztés alatt localhost, élesben a `https://smaash-web.onrender.com` cím aktív.

```csharp
[SerializeField] private bool useLocalhost = false;
[SerializeField] private string localhostUrl = "http://localhost:8080";
[SerializeField] private string deployedUrl = "https://smaash-web.onrender.com";

public string BaseUrl => (useLocalhost ? localhostUrl : deployedUrl).TrimEnd('/');
```

### 6.1 Bejelentkezés – `Login()`

A bejelentkezési kérés egy `UnityWebRequest` POST hívás JSON törzzsel. Siker esetén a kapott JWT tokeneket a `PlayerPrefs`-be menti, amelyek az alkalmazás újraindítása után is elérhetők.

```csharp
private IEnumerator Login(string email, string password, Action<bool, string> done)
{
    var json = JsonUtility.ToJson(new GameLoginRequest { email = email, password = password });

    using var req = new UnityWebRequest($"{BaseUrl}/api/game-login", "POST");
    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
    req.downloadHandler = new DownloadHandlerBuffer();
    req.SetRequestHeader("Content-Type", "application/json");

    yield return req.SendWebRequest();

    if (req.result != UnityWebRequest.Result.Success)
    {
        done(false, req.downloadHandler.text);
        yield break;
    }

    var resp = JsonUtility.FromJson<GameLoginResponse>(req.downloadHandler.text);
    SaveTokens(resp.accessToken, resp.refreshToken);
    done(true, "");
}
```

### 6.2 Automatikus bejelentkezés és token megújítás

Alkalmazás indításakor a `TryAutoLogin()` ellenőrzi a mentett tokeneket. Ha az access token még érvényes, egyenesen a profilválasztóba lép; ha lejárt de van refresh token, megpróbálja megújítani:

```csharp
private IEnumerator TryAutoLogin()
{
    string savedAccess = PlayerPrefs.GetString(AccessKey, "");
    string savedRefresh = PlayerPrefs.GetString(RefreshKey, "");

    if (string.IsNullOrEmpty(savedAccess) && string.IsNullOrEmpty(savedRefresh))
        yield break;

    // Ha az access token még érvényes, nem kell bejelentkezni
    if (!string.IsNullOrEmpty(savedAccess) && IsJwtNotExpired(savedAccess))
    {
        AccessToken = savedAccess;
        if (SceneManager.GetActiveScene().name != profileSelectScene)
            SceneManager.LoadScene(profileSelectScene);
        yield break;
    }

    // Access token lejárt → megpróbálja refresh tokennel megújítani
    if (!string.IsNullOrEmpty(savedRefresh))
    {
        bool refreshed = false;
        yield return RefreshToken(ok => refreshed = ok);

        if (refreshed && SceneManager.GetActiveScene().name != profileSelectScene)
            SceneManager.LoadScene(profileSelectScene);
    }
}

private IEnumerator RefreshToken(Action<bool> done)
{
    string currentRefresh = PlayerPrefs.GetString(RefreshKey, "");
        
    if (string.IsNullOrEmpty(currentRefresh))
    {
        done?.Invoke(false);
        yield break;
    }

    var json = JsonUtility.ToJson(new RefreshRequestDto { refreshToken = currentRefresh });

    using var req = new UnityWebRequest($"{BaseUrl}/api/game-refresh", "POST");
    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
    req.downloadHandler = new DownloadHandlerBuffer();
    req.SetRequestHeader("Content-Type", "application/json");

    yield return req.SendWebRequest();

    if (req.result != UnityWebRequest.Result.Success || req.responseCode != 200)
    {
        ClearTokens();
        done?.Invoke(false);
        yield break;
    }

    var resp = JsonUtility.FromJson<RefreshResponseDto>(req.downloadHandler.text);
    SaveTokens(resp.accessToken, resp.refreshToken);
    done?.Invoke(true);
}
```

A token lejáratát a JWT payload `exp` mezőjéből ellenőrzi, base64 dekódolással:

```csharp
private bool IsJwtNotExpired(string jwt)
{
    var parts = jwt.Split('.');
    if (parts.Length < 2) return false;
    try
    {
        var payload = JsonUtility.FromJson<JwtPayload>(DecodeBase64Url(parts[1]));
        return payload != null && payload.exp > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
    catch { return false; }
}

private string DecodeBase64Url(string s)
{
    // JWT base64url formátum: '+' helyett '-', '/' helyett '_', padding nélkül
    s = s.Replace('-', '+').Replace('_', '/');
    switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
    return Encoding.UTF8.GetString(Convert.FromBase64String(s));
}
```

### 6.3 Profilok lekérése – `GetMyProfiles()`

A profil lista lekérésekor először a JWT-ből kinyeri a felhasználói azonosítót, majd autorizált GET kérést küld:

```csharp
public IEnumerator GetMyProfiles(Action<bool, PlayerProfileDto[]> done)
{
    string token = AccessToken;
    if (string.IsNullOrEmpty(token))
        token = PlayerPrefs.GetString(AccessKey, "");

    // User ID kinyerése a JWT sub mezőjéből
    int userId = GetUserIdFromToken(token);
    if (userId < 0) { done(false, null); yield break; }

    using var req = UnityWebRequest.Get($"{BaseUrl}/api/users/{userId}/profiles");
    req.SetRequestHeader("Authorization", $"Bearer {token}");

    yield return req.SendWebRequest();

    if (req.result != UnityWebRequest.Result.Success)
    {
        done(false, null);
        yield break;
    }

    var rawJson = req.downloadHandler.text?.Trim();
    PlayerProfileDto[] profiles;

    // A backend válasz lehet sima JSON tömb vagy wrapper objektum – mindkettőt kezeli
    if (!string.IsNullOrEmpty(rawJson) && rawJson.StartsWith("["))
        profiles = JsonHelper.FromJsonArray<PlayerProfileDto>(rawJson);
    else
    {
        var resp = JsonUtility.FromJson<PlayerProfileListResponse>(rawJson);
        profiles = resp != null ? resp.profiles : Array.Empty<PlayerProfileDto>();
    }

    done(true, profiles);
}
```

A `GetUserIdFromToken()` a JWT közepső (payload) szegmensét dekódolja és kinyeri a `sub` mezőt:

```csharp
private int GetUserIdFromToken(string token)
{
    var parts = token.Split('.');
    if (parts.Length < 2) return -1;

    try
    {
        var json = DecodeBase64Url(parts[1]);
        var payload = JsonUtility.FromJson<GameJwtPayload>(json);
        if (payload == null || payload.sub <= 0) return -1;
        return (int)payload.sub;
    }
    catch { return -1; }
}
```

### 6.4 Általános autorizált POST – `PostAuthorizedJson<TPayload>()`

Ez a metódus újrafelhasználható minden olyan kéréshez, amely JWT tokent igényel (pl. meccs eredmény beküldés):

```csharp
public IEnumerator PostAuthorizedJson<TPayload>(string endpoint, TPayload payload, Action<bool, string> done)
{
    string token = AccessToken;
    if (string.IsNullOrWhiteSpace(token))
        token = PlayerPrefs.GetString(AccessKey, "");

    string json = JsonUtility.ToJson(payload);

    using var req = new UnityWebRequest($"{BaseUrl}{endpoint}", UnityWebRequest.kHttpVerbPOST);
    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
    req.downloadHandler = new DownloadHandlerBuffer();
    req.SetRequestHeader("Content-Type", "application/json");
    req.SetRequestHeader("Accept", "application/json");
    req.SetRequestHeader("Authorization", $"Bearer {token}");   // JWT Bearer token

    yield return req.SendWebRequest();

    bool ok = req.result == UnityWebRequest.Result.Success
              && req.responseCode >= 200 && req.responseCode < 300;
    string body = req.downloadHandler?.text ?? $"HTTP {req.responseCode}";

    done?.Invoke(ok, body);
}
```

### 6.5 `JsonHelper`

A Unity `JsonUtility` nem tudja kezelni a gyökér szintű JSON tömböket (`[{...},{...}]`). A `JsonHelper` wrapper objektumba csomagolja a tömböt, majd azt használja:

```csharp
public static T[] FromJsonArray<T>(string json)
{
    if (string.IsNullOrEmpty(json)) return Array.Empty<T>();

    // Unity JsonUtility trükk: a tömböt egy {"items": [...]} objektumba csomagolja
    var wrapped = "{\"items\":" + json + "}";
    var result = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
    return result != null && result.items != null ? result.items : Array.Empty<T>();
}
```

### 6.6 Profilkép betöltése – `LoadAvatarFromUri()`

A profilképeket a backend külön végpontján éri el a játék, autorizált textúra kérésekkel:

```csharp
private IEnumerator LoadAvatarFromUri(string uri, Image targetImage)
{
    using var request = UnityWebRequestTexture.GetTexture(uri);

    string token = authClient != null ? authClient.AccessToken : string.Empty;
    if (string.IsNullOrWhiteSpace(token))
        token = PlayerPrefs.GetString("access_token", "");

    if (!string.IsNullOrWhiteSpace(token))
        request.SetRequestHeader("Authorization", $"Bearer {token}");

    request.SetRequestHeader("Accept", "image/*");
    yield return request.SendWebRequest();

    if (request.result != UnityWebRequest.Result.Success) yield break;

    // A letöltött textúrából Unity Sprite-ot hoz létre
    var texture = DownloadHandlerTexture.GetContent(request);
    var sprite = Sprite.Create(
        texture,
        new Rect(0f, 0f, texture.width, texture.height),
        new Vector2(0.5f, 0.5f),
        100f
    );

    targetImage.sprite = sprite;
    targetImage.preserveAspect = true;
}
```

---

## 7. Karakterválasztás

### 7.1 `Character_Database` (ScriptableObject)

ScriptableObject-ként létrehozott asset, amely az összes karaktert tartalmazza. A Unity Editorban szerkeszthető, és a `NetworkHandler` is hivatkozik rá a spawn során.

| Mező/Tulajdonság | Típus | Leírás |
|---|---|---|
| `character[]` | `Character[]` | Karakterek tömbje |
| `CharacterCount` | `int` (get) | Karakterek száma |
| `GetCharacter(index)` | `Character` | Adott indexű karakter visszaadása |

### 7.2 `Character` (adatosztály)

| Mező | Típus | Leírás |
|---|---|---|
| `character_id` | `int` | Backend adatbázis azonosítója |
| `characterSprite` | `Sprite` | Karakterválasztón megjelenő kép |
| `character_name` | `string` | Karakter neve |
| `playerPrefab` | `NetworkPrefabRef` | Fusion által kezelt prefab referencia (nem sima `GameObject`) |

### 7.3 `CharacterManager`

A karakterválasztó képernyő vezérlője. A `NextOption()` és `BackOption()` metódusok körbejárással lépteti a karaktereket és menti a választást `PlayerPrefs`-be:

```csharp
public void NextOption()
{
    selectedOption++;
    if (selectedOption >= characterDatabase.CharacterCount)
        selectedOption = 0;
    UpdateCharacter(selectedOption);
    Save();
}

private void UpdateCharacter(int selectedOption)
{
    Character character = characterDatabase.GetCharacter(selectedOption);
    artworkSprite.sprite = character.characterSprite;
    nameText.text = character.character_name;
}

private void Save()
{
    PlayerPrefs.SetInt("selectedOption", selectedOption);
    PlayerPrefs.SetString("character_name", nameText.text);
    PlayerPrefs.Save();
}
```