using System.Collections.Generic;
using UnityEngine;

// FELPUDO'S RACE - O resgate da Fofura
// Script unico: cria fundo com paralaxe, carro, inimigos, frutas, tiros,
// vidas, vitoria, derrota e telas. Basta colocar este script em um GameObject vazio.
[System.Serializable]
public class FrutaSlot
{
    public string nome;            // nao mude este nome
    public Sprite[] parada;        // imagens *_Idle desta fruta
    public Sprite[] explodindo;    // imagens *_Explode desta fruta

    public FrutaSlot(string n) { nome = n; }
}

public class FelpudoRace : MonoBehaviour
{
    [Header("Fase")]
    public int frutasParaVencer = 15;      // CONDICAO DE VITORIA
    public float velocidadeMundo = 5f;
    public float alturaChao = -4f;         // Y onde as rodas encostam na estrada
    public float tamanhoCamera = 5f;       // maior = tudo fica menor na tela (zoom out)
    public float escalaFundo = 1f;         // 1 = fundo ocupa a altura da tela; menor = fundo menor
    public float deslocYFundo = 0f;        // sobe (+) ou desce (-) o fundo inteiro

    [Header("Felpudo (carro)")]
    public float alturaCarro = 2f;
    public float gravidade = 28f;
    public float forcaPulo = 10f;
    public int vidasMax = 3;               // CONDICAO DE DERROTA: chegar a 0 vidas
    public float intervaloTiro = 0.4f;
    public float velocidadeTiro = 14f;

    [Header("IMAGENS DO FUNDO (paralaxe): arraste 1 imagem em cada campo")]
    public Sprite fundoCeu;          // 9. Background
    public Sprite fundoNuvens1;      // 6. Nuvens 1
    public Sprite fundoNuvens2;      // 7. Nuvens 2
    public Sprite fundoNevoa;        // 5. Nevoa
    public Sprite fundoPredios;      // 8. Predios
    public Sprite fundoCasasFundo;   // 4. Casas Fundo
    public Sprite fundoCasasFrente;  // 3. Casas Frente
    public Sprite fundoMuro;         // 1. Muro
    public Sprite fundoChao;         // 2. Chao

    [Header("IMAGENS DO FELPUDO: selecione todas as imagens da pasta e arraste para o nome do campo")]
    public Sprite[] carroCorrendo;   // pasta corre
    public Sprite[] carroPulando;    // pasta pulando
    public Sprite[] carroCaindo;     // pasta caindo
    public Sprite[] carroAbaixando;  // pasta abaixa

    [Header("IMAGENS DOS INIMIGOS")]
    public Sprite[] lesmoAndando;    // pasta lesmoWalk
    public Sprite[] barrilRolando;   // pasta lesmo-left

    [Header("IMAGENS DAS FRUTAS (7 itens, um por fruta)")]
    public FrutaSlot[] frutasImagens = new FrutaSlot[]
    {
        new FrutaSlot("Avocado"), new FrutaSlot("Banana"), new FrutaSlot("Lemon"),
        new FrutaSlot("Orange"), new FrutaSlot("Pear"), new FrutaSlot("PineApple"), new FrutaSlot("Tomato")
    };

    [Header("IMAGEM DO TIRO (opcional: vazio = usa a banana)")]
    public Sprite imagemTiro;

    enum Estado { Intro, Jogando, Venceu, Perdeu }

    class Camada
    {
        public Transform[] tiles;
        public float larg;
        public float fator;
        public float deriva;
    }

    class Ente
    {
        public Transform t, vis;
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float escala, larg, alt, anim, flash, proprio;
        public Vector2 vel, tam, off;
        public bool baseado;
        public int tipo, vida;
        public string nome;
        public Color cor = Color.white;
    }

    class Efeito
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float vida, total, fps;
        public Sprite[] frames;
    }

    Camera cam;
    float halfW, halfH;
    Estado estado = Estado.Intro;
    GameObject raiz;

    readonly List<Camada> camadas = new List<Camada>();
    readonly List<Ente> inimigos = new List<Ente>();
    readonly List<Ente> frutas = new List<Ente>();
    readonly List<Ente> tiros = new List<Ente>();
    readonly List<Efeito> efeitos = new List<Efeito>();

    Ente jogador;
    GameObject escudoVis;

    Sprite[] fCorre, fPula, fCai, fAbaixa, fLesmo, fBarril, fTiro;
    readonly Dictionary<string, Sprite[]> fIdle = new Dictionary<string, Sprite[]>();
    readonly Dictionary<string, Sprite[]> fExplode = new Dictionary<string, Sprite[]>();
    readonly string[] nomesFrutas = { "Avocado", "Banana", "Lemon", "Orange", "Pear", "PineApple", "Tomato" };

    float vy, proxTiro, jumpCd, tSpawnIni, tSpawnFruta, invuln, escudo, turbo, tAviso;
    int vidas, pontos, coletadas;
    bool noChao, agachado;
    string aviso = "";

    static Sprite sprQ, sprC;

    // ------------------------------------------------------------ INICIO

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Nao achei a Main Camera (precisa ter a tag MainCamera).");
            enabled = false;
            return;
        }

        cam.orthographic = true;
        cam.orthographicSize = tamanhoCamera;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.38f, 0.7f);

        fCorre = Pega(carroCorrendo, "endlessRace/corre", null);
        fPula = Pega(carroPulando, "endlessRace/pulando", null);
        fCai = Pega(carroCaindo, "endlessRace/caindo", null);
        fAbaixa = Pega(carroAbaixando, "endlessRace/abaixa", null);
        fLesmo = Pega(lesmoAndando, "lesmoWalk", null);
        fBarril = Pega(barrilRolando, "lesmo-left", null);

        foreach (string n in nomesFrutas)
        {
            Sprite[] pa = null, ex = null;
            if (frutasImagens != null)
            {
                foreach (FrutaSlot fs in frutasImagens)
                {
                    if (fs != null && fs.nome == n) { pa = fs.parada; ex = fs.explodindo; }
                }
            }
            fIdle[n] = Pega(pa, "fruitCrush/" + n, "Idle");
            fExplode[n] = Pega(ex, "fruitCrush/" + n, "Explode");
        }

        Sprite[] ban = fIdle["Banana"];
        if (imagemTiro != null) fTiro = new Sprite[] { imagemTiro };
        else fTiro = (ban != null && ban.Length > 0) ? new Sprite[] { ban[0] } : null;

        Iniciar(Estado.Intro);
    }

    // usa as imagens arrastadas no Inspector; se o campo estiver vazio, tenta Assets/Resources
    static Sprite[] Pega(Sprite[] slot, string pastaResources, string filtro)
    {
        if (slot != null)
        {
            List<Sprite> l = new List<Sprite>();
            foreach (Sprite sp in slot)
            {
                if (sp == null) continue;
                if (filtro != null && !sp.name.Contains(filtro)) continue;
                l.Add(sp);
            }
            if (l.Count > 0)
            {
                l.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                return l.ToArray();
            }
        }
        return Carregar(pastaResources, filtro);
    }

    static Sprite[] Carregar(string pasta, string filtro)
    {
        Sprite[] todos = Resources.LoadAll<Sprite>(pasta);
        List<Sprite> l = new List<Sprite>();
        foreach (Sprite s in todos)
            if (filtro == null || s.name.Contains(filtro)) l.Add(s);

        if (l.Count == 0)
        {
            Debug.LogWarning("Campo de imagens vazio (" + pasta + "): usando forma colorida no lugar.");
            return null;
        }
        l.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return l.ToArray();
    }

    void Iniciar(Estado novo)
    {
        if (raiz != null) Destroy(raiz);
        raiz = new GameObject("Mundo");

        camadas.Clear();
        inimigos.Clear();
        frutas.Clear();
        tiros.Clear();
        efeitos.Clear();

        AtualizarTela();
        CriarFundo();

        jogador = CriarEnte("Felpudo", fCorre, alturaCarro, true,
            new Vector2(PosX(), alturaChao), 10, new Color(0.85f, 0.2f, 0.2f), false, 0.7f, 0.75f);

        escudoVis = new GameObject("Escudo");
        escudoVis.transform.SetParent(jogador.t, false);
        escudoVis.transform.localPosition = new Vector3(0f, alturaCarro * 0.5f, 0f);
        escudoVis.transform.localScale = Vector3.one * alturaCarro * 1.6f;
        SpriteRenderer es = escudoVis.AddComponent<SpriteRenderer>();
        es.sprite = Circulo();
        es.color = new Color(0.3f, 0.65f, 1f, 0.35f);
        es.sortingOrder = 11;
        escudoVis.SetActive(false);

        vy = 0f;
        proxTiro = 0f;
        jumpCd = 0f;
        tSpawnIni = 2f;
        tSpawnFruta = 1f;
        invuln = 0f;
        escudo = 0f;
        turbo = 0f;
        tAviso = 0f;
        vidas = vidasMax;
        pontos = 0;
        coletadas = 0;
        noChao = true;
        agachado = false;
        aviso = "";

        estado = novo;
    }

    float PosX() { return -halfW * 0.55f; }

    void AtualizarTela()
    {
        halfH = cam.orthographicSize;
        halfW = halfH * cam.aspect;
    }

    // ------------------------------------------------------------ FUNDO (PARALAXE)

    void CriarFundo()
    {
        Sprite[] todos = Resources.LoadAll<Sprite>("Background-divided");
        Sprite[] slots = { fundoCeu, fundoNuvens1, fundoNuvens2, fundoNevoa, fundoPredios,
                           fundoCasasFundo, fundoCasasFrente, fundoMuro, fundoChao };

        // de tras para frente
        string[] num = { "9.", "6.", "7.", "5.", "8.", "4.", "3.", "1.", "2." };
        float[] fator = { 0f, 0.03f, 0.06f, 0.1f, 0.18f, 0.3f, 0.5f, 0.85f, 1f };
        float[] deriva = { 0f, 0.35f, 0.2f, 0f, 0f, 0f, 0f, 0f, 0f };

        // Montagem igual a imagem de referencia (altura total = 1136 px).
        // Para cada camada: onde ela comeca (base) e quanto ela ocupa (altura),
        // como fracao da altura da tela. 0 = parte de baixo da tela, 1 = topo.
        //                      ceu    nuv1   nuv2   nevoa  predios casasF casasFr muro   chao
        // (casas escuras do fundo: base 0.170, um pouco mais baixas que antes)
        float[] baseFr = { 0.000f, 0.340f, 0.331f, 0.384f, 0.234f, 0.170f, 0.225f, 0.080f, 0.000f };
        // palavras do nome da imagem, usadas se o numero nao bater
        string[] palavras = { "ceu", "nuvens 1", "nuvens 2", "nevoa", "predios", "casas fundo", "casas frente", "muro", "chao" };
        float[] alturaFr = { 1.000f, 0.453f, 0.405f, 0.194f, 0.387f, 0.220f, 0.158f, 0.163f, 0.080f };

        if (todos.Length == 0)
            Debug.LogWarning("Fundo: a pasta Assets/Resources/Background-divided nao tem imagens. Arraste as imagens nos campos do Inspector.");

        // 1) escolhe a imagem de cada camada
        Sprite[] escolhidas = new Sprite[num.Length];
        for (int i = 0; i < num.Length; i++)
        {
            Sprite sp = slots[i];
            if (sp == null) sp = AchaFundo(todos, num[i], palavras[i]);
            escolhidas[i] = sp;
        }

        for (int i = 0; i < num.Length; i++)
        {
            Sprite sp = escolhidas[i];
            bool reserva = false;
            if (sp == null)
            {
                Debug.LogWarning("Fundo: nao achei a imagem da camada " + num[i] + " (arraste no campo do Inspector).");
                if (i != num.Length - 1) continue;
                sp = Quadrado();      // chao de reserva: faixa de cor lisa
                reserva = true;
            }

            // 2) cada camada ganha o tamanho que tem na referencia (mantendo a proporcao da propria imagem)
            float ppu = sp.pixelsPerUnit;
            float escala = (halfH * 2f * alturaFr[i] * escalaFundo) / (sp.rect.height / ppu);

            // tamanho REAL da imagem (rect), e nao bounds, que e cortado nas partes transparentes
            float larg = sp.rect.width / ppu * escala;
            float alt = sp.rect.height / ppu * escala;

            // do pivo da imagem ate o centro dela
            Vector3 ate = new Vector3((sp.rect.width * 0.5f - sp.pivot.x) / ppu * escala,
                                      (sp.rect.height * 0.5f - sp.pivot.y) / ppu * escala, 0f);

            // camada na altura da referencia (base + metade da altura)
            float yCentro = -halfH + baseFr[i] * halfH * 2f * escalaFundo + alt * 0.5f + deslocYFundo;

            int n = Mathf.CeilToInt(halfH * 3f * 2f / larg) + 2;

            Camada c = new Camada();
            c.larg = larg;
            c.fator = fator[i];
            c.deriva = deriva[i];
            c.tiles = new Transform[n];

            for (int k = 0; k < n; k++)
            {
                // o objeto "Camada" fica no CENTRO da imagem; o desenho e filho dele
                GameObject go = new GameObject("Camada_" + num[i] + k);
                go.transform.SetParent(raiz.transform, false);
                go.transform.position = new Vector3(-halfH * 3f + k * larg, yCentro, 0f);

                GameObject img = new GameObject("img");
                img.transform.SetParent(go.transform, false);
                img.transform.localScale = Vector3.one * escala;
                img.transform.localPosition = -ate;
                SpriteRenderer sr = img.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                if (reserva) sr.color = new Color(0.42f, 0.46f, 0.3f);
                // a nevoa (i = 3) fica NA FRENTE dos predios e atras das casas, como na referencia
                sr.sortingOrder = (i == 3) ? -11 : -20 + i * 2;

                c.tiles[k] = go.transform;
            }
            camadas.Add(c);
        }
    }

    // procura a imagem da camada: pelo numero ("5.") ou por palavras do nome (sem acento, sem maiuscula)
    static Sprite AchaFundo(Sprite[] todos, string numero, string palavra)
    {
        foreach (Sprite s in todos)
            if (s.name.StartsWith(numero)) return s;

        string alvo = palavra.Replace(" ", "");
        foreach (Sprite s in todos)
        {
            string nm = s.name.ToLowerInvariant().Replace("é", "e").Replace("ã", "a").Replace("ç", "c")
                              .Replace("í", "i").Replace("ú", "u").Replace(" ", "").Replace("_", "").Replace("-", "");
            if (nm.Contains(alvo)) return s;
        }
        return null;
    }

    void AtualizarFundo(float v, float dt)
    {
        foreach (Camada c in camadas)
        {
            float d = (v * c.fator + c.deriva) * dt;
            float total = c.larg * c.tiles.Length;
            foreach (Transform tr in c.tiles)
            {
                Vector3 p = tr.position;
                p.x -= d;
                if (p.x + c.larg * 0.5f < -halfW - 1f) p.x += total;
                tr.position = p;
            }
        }
    }

    // ------------------------------------------------------------ UPDATE

    void Update()
    {
        AtualizarTela();
        float dt = Time.deltaTime;

        if (estado == Estado.Intro)
        {
            AtualizarFundo(velocidadeMundo * 0.4f, dt);
            jogador.t.position = new Vector3(PosX(), alturaChao, 0f);
            Anima(jogador, dt, 14f);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                Iniciar(Estado.Jogando);
            return;
        }

        if (estado != Estado.Jogando)
        {
            AtualizarEfeitos(dt);
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
                Iniciar(Estado.Jogando);
            return;
        }

        float vm = velocidadeMundo * (turbo > 0f ? 1.7f : 1f);

        escudo = Mathf.Max(0f, escudo - dt);
        turbo = Mathf.Max(0f, turbo - dt);
        invuln -= dt;
        tAviso -= dt;

        AtualizarFundo(vm, dt);
        AtualizarJogador(dt);

        tSpawnIni -= dt;
        if (tSpawnIni <= 0f)
        {
            GerarInimigo();
            float k = Mathf.Clamp01(coletadas / (float)Mathf.Max(1, frutasParaVencer));
            tSpawnIni = Random.Range(Mathf.Lerp(1.8f, 1.1f, k), Mathf.Lerp(3.2f, 2f, k));
        }

        tSpawnFruta -= dt;
        if (tSpawnFruta <= 0f)
        {
            GerarFruta();
            tSpawnFruta = Random.Range(1.1f, 1.9f);
        }

        AtualizarInimigos(dt, vm);
        AtualizarFrutas(dt, vm);
        AtualizarTiros(dt);
        AtualizarEfeitos(dt);
    }

    // ------------------------------------------------------------ JOGADOR

    void AtualizarJogador(float dt)
    {
        jumpCd -= dt;

        bool pula = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)
                 || Input.GetKeyDown(KeyCode.W) || Input.GetMouseButtonDown(0);
        agachado = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

        if (pula && jumpCd <= 0f)
        {
            vy = forcaPulo;
            jumpCd = 0.1f;
        }

        vy -= gravidade * dt;
        if (agachado && !noChao) vy -= gravidade * dt; // mergulho rapido

        Vector3 p = jogador.t.position;
        p.x = PosX();
        p.y += vy * dt;

        if (p.y <= alturaChao)
        {
            p.y = alturaChao;
            vy = 0f;
            noChao = true;
        }
        else
        {
            noChao = false;
        }

        float teto = halfH - alturaCarro;
        if (p.y > teto)
        {
            p.y = teto;
            if (vy > 0f) vy = 0f;
        }
        jogador.t.position = p;

        // caixa de colisao (menor quando agachado)
        bool baixo = agachado && noChao;
        jogador.tam.y = jogador.alt * (baixo ? 0.5f : 0.75f);
        jogador.off.y = baixo ? jogador.tam.y * 0.5f : jogador.alt * 0.5f;

        // animacao
        Sprite[] alvo = fCorre;
        if (!noChao) alvo = vy > 0f ? (fPula != null ? fPula : fCorre) : (fCai != null ? fCai : fCorre);
        else if (agachado && fAbaixa != null) alvo = fAbaixa;
        if (alvo != null && jogador.frames != alvo)
        {
            jogador.frames = alvo;
            jogador.anim = 0f;
        }
        Anima(jogador, dt, noChao ? 14f : 12f);

        // piscar quando machucado
        if (invuln > 0f) jogador.sr.enabled = ((int)(invuln * 12f) % 2) == 0;
        else jogador.sr.enabled = true;

        // escudo
        escudoVis.SetActive(escudo > 0f && (escudo > 1.5f || ((int)(escudo * 8f) % 2) == 0));

        // ataque
        if ((Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetMouseButtonDown(1))
            && Time.time >= proxTiro)
        {
            proxTiro = Time.time + intervaloTiro;
            Atirar();
        }
    }

    void Atirar()
    {
        Vector2 pos = (Vector2)jogador.t.position + new Vector2(jogador.larg * 0.5f, jogador.alt * 0.55f);
        Ente t = CriarEnte("Tiro", fTiro, 0.5f, false, pos, 8, new Color(1f, 0.85f, 0.1f), true, 0.9f, 0.9f);
        t.vel = new Vector2(velocidadeTiro, 0f);
        tiros.Add(t);
    }

    void DanoJogador()
    {
        if (invuln > 0f) return;

        Vector2 centro = (Vector2)jogador.t.position + jogador.off;

        if (escudo > 0f)
        {
            invuln = 0.5f;
            Burst(centro, new Color(0.4f, 0.7f, 1f), 8);
            return;
        }

        vidas--;
        invuln = 1.5f;
        vy = forcaPulo * 0.6f;
        Burst(centro, new Color(0.9f, 0.1f, 0.15f), 14);

        if (vidas <= 0)
        {
            vidas = 0;
            jogador.sr.enabled = true;
            estado = Estado.Perdeu;
        }
    }

    // ------------------------------------------------------------ INIMIGOS

    void GerarInimigo()
    {
        bool barril = Random.value < 0.4f;
        Vector2 pos = new Vector2(halfW + 2f, alturaChao);
        Ente e;

        if (barril)
        {
            // Barril: nao leva dano ao ser pisado, so de tiros (3 acertos). Rapido.
            e = CriarEnte("Barril", fBarril, 1.5f, true, pos, 5, new Color(0.6f, 0.35f, 0.1f), false, 0.8f, 0.8f);
            e.tipo = 1;
            e.vida = 3;
            e.proprio = 3.5f;
        }
        else
        {
            // Lesmo: morre pisando em cima (1 pulo) ou com 2 tiros. Lento.
            e = CriarEnte("Lesmo", fLesmo, 1.4f, true, pos, 5, new Color(0.3f, 0.3f, 0.9f), true, 0.7f, 0.8f);
            e.tipo = 0;
            e.vida = 2;
            e.proprio = 1.5f;
        }
        inimigos.Add(e);
    }

    void AtualizarInimigos(float dt, float vm)
    {
        for (int i = inimigos.Count - 1; i >= 0; i--)
        {
            Ente e = inimigos[i];
            e.t.position += new Vector3(-(vm + e.proprio) * dt, 0f, 0f);
            Anima(e, dt, 10f);
            Pisca(e, dt);

            if (e.t.position.x < -halfW - 3f)
            {
                Remover(inimigos, i);
                continue;
            }

            Rect re = R(e);
            if (!re.Overlaps(R(jogador))) continue;

            bool pisao = e.tipo == 0 && vy < -1f && jogador.t.position.y > re.center.y;
            if (pisao)
            {
                Burst(re.center, new Color(0.3f, 0.4f, 1f), 14);
                Remover(inimigos, i);
                Pontua(20);
                vy = forcaPulo * 0.85f;
                continue;
            }

            DanoJogador();
            if (estado != Estado.Jogando) return;
        }
    }

    // ------------------------------------------------------------ FRUTAS

    void GerarFruta()
    {
        string nome = nomesFrutas[Random.Range(0, nomesFrutas.Length)];
        Sprite[] fr = fIdle[nome];
        float y = Random.Range(alturaChao + 1.1f, halfH - 1f);
        Ente f = CriarEnte("Fruta_" + nome, fr, 0.9f, false, new Vector2(halfW + 1f, y), 3,
            CorFruta(nome), true, 0.8f, 0.8f);
        f.nome = nome;
        frutas.Add(f);
    }

    void AtualizarFrutas(float dt, float vm)
    {
        for (int i = frutas.Count - 1; i >= 0; i--)
        {
            Ente f = frutas[i];
            f.t.position += new Vector3(-vm * dt, 0f, 0f);
            Anima(f, dt, 10f);

            if (f.t.position.x < -halfW - 2f)
            {
                Remover(frutas, i);
                continue;
            }

            if (!R(f).Overlaps(R(jogador))) continue;

            Coletar(f);
            Remover(frutas, i);
            if (estado != Estado.Jogando) return;
        }
    }

    void Coletar(Ente f)
    {
        coletadas++;
        Pontua(10);

        Vector2 pos = f.t.position;
        Sprite[] ex = fExplode[f.nome];
        if (ex != null)
        {
            Efeito ef = NovoEfeito(pos, ex[0], f.escala);
            ef.frames = ex;
            ef.fps = 14f;
            ef.total = ef.vida = ex.Length / 14f;
        }
        else
        {
            Burst(pos, CorFruta(f.nome), 8);
        }

        if (f.nome == "PineApple")
        {
            escudo = 6f;
            Aviso("ESCUDO! (6s)");
        }
        else if (f.nome == "Lemon")
        {
            turbo = 5f;
            Aviso("TURBO! Velocidade maior e pontos em dobro (5s)");
        }
        else if (vidas < vidasMax)
        {
            vidas++;
            Aviso("+1 vida");
        }
        else
        {
            Pontua(10);
        }

        if (coletadas >= frutasParaVencer) estado = Estado.Venceu;
    }

    // ------------------------------------------------------------ TIROS

    void AtualizarTiros(float dt)
    {
        for (int i = tiros.Count - 1; i >= 0; i--)
        {
            Ente t = tiros[i];
            t.t.position += (Vector3)(t.vel * dt);
            t.vis.Rotate(0f, 0f, -420f * dt);

            if (t.t.position.x > halfW + 2f)
            {
                Remover(tiros, i);
                continue;
            }

            Rect rt = R(t);
            bool acertou = false;

            for (int j = inimigos.Count - 1; j >= 0; j--)
            {
                Ente e = inimigos[j];
                if (!rt.Overlaps(R(e))) continue;

                e.vida--;
                e.flash = 0.1f;
                Burst(rt.center, new Color(1f, 0.85f, 0.1f), 5);

                if (e.vida <= 0)
                {
                    Burst(R(e).center, e.tipo == 1 ? new Color(0.6f, 0.35f, 0.1f) : new Color(0.3f, 0.4f, 1f), 14);
                    Remover(inimigos, j);
                    Pontua(e.tipo == 1 ? 30 : 20);
                }
                acertou = true;
                break;
            }

            if (acertou) Remover(tiros, i);
        }
    }

    // ------------------------------------------------------------ EFEITOS / PARTICULAS

    Efeito NovoEfeito(Vector2 pos, Sprite sp, float escala)
    {
        GameObject go = new GameObject("Efeito");
        go.transform.SetParent(raiz.transform, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * escala;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = 12;

        Efeito e = new Efeito();
        e.go = go;
        e.sr = sr;
        e.vida = e.total = 0.5f;
        efeitos.Add(e);
        return e;
    }

    void Burst(Vector2 pos, Color cor, int n)
    {
        for (int i = 0; i < n; i++)
        {
            Efeito e = NovoEfeito(pos, Circulo(), Random.Range(0.1f, 0.3f));
            e.sr.color = cor;
            e.vel = Random.insideUnitCircle.normalized * Random.Range(2f, 6f);
            e.vida = e.total = Random.Range(0.35f, 0.7f);
        }
    }

    void AtualizarEfeitos(float dt)
    {
        for (int i = efeitos.Count - 1; i >= 0; i--)
        {
            Efeito e = efeitos[i];
            e.vida -= dt;
            if (e.vida <= 0f || e.go == null)
            {
                if (e.go != null) Destroy(e.go);
                efeitos.RemoveAt(i);
                continue;
            }

            e.go.transform.position += (Vector3)(e.vel * dt);

            if (e.frames != null)
            {
                int k = Mathf.Clamp((int)((e.total - e.vida) * e.fps), 0, e.frames.Length - 1);
                e.sr.sprite = e.frames[k];
            }
            else
            {
                Color c = e.sr.color;
                c.a = Mathf.Clamp01(e.vida / e.total);
                e.sr.color = c;
            }
        }
    }

    // ------------------------------------------------------------ AJUDAS

    Ente CriarEnte(string nome, Sprite[] frames, float altura, bool baseado, Vector2 pos, int ordem,
                   Color corFallback, bool circulo, float fw, float fh)
    {
        Ente e = new Ente();

        GameObject go = new GameObject(nome);
        go.transform.SetParent(raiz.transform, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        e.t = go.transform;

        GameObject v = new GameObject("vis");
        v.transform.SetParent(go.transform, false);
        e.vis = v.transform;
        e.sr = v.AddComponent<SpriteRenderer>();
        e.sr.sortingOrder = ordem;
        e.baseado = baseado;

        Sprite s0;
        if (frames != null && frames.Length > 0)
        {
            e.frames = frames;
            s0 = frames[0];
            e.cor = Color.white;
        }
        else
        {
            s0 = circulo ? Circulo() : Quadrado();
            e.frames = new Sprite[] { s0 };
            e.cor = corFallback;
        }

        e.escala = altura / s0.bounds.size.y;
        e.larg = s0.bounds.size.x * e.escala;
        e.alt = s0.bounds.size.y * e.escala;
        e.tam = new Vector2(e.larg * fw, e.alt * fh);
        e.off = new Vector2(0f, baseado ? e.alt * 0.5f : 0f);

        Pintar(e, s0);
        e.sr.color = e.cor;
        return e;
    }

    void Pintar(Ente e, Sprite s)
    {
        e.sr.sprite = s;
        e.vis.localScale = new Vector3(e.escala, e.escala, 1f);
        float y = e.baseado ? -s.bounds.min.y * e.escala : 0f;
        e.vis.localPosition = new Vector3(0f, y, 0f);
    }

    void Anima(Ente e, float dt, float fps)
    {
        e.anim += dt;
        int k = ((int)(e.anim * fps)) % e.frames.Length;
        Pintar(e, e.frames[k]);
    }

    void Pisca(Ente e, float dt)
    {
        if (e.flash > 0f)
        {
            e.flash -= dt;
            e.sr.color = Color.red;
        }
        else
        {
            e.sr.color = e.cor;
        }
    }

    Rect R(Ente e)
    {
        Vector2 c = (Vector2)e.t.position + e.off;
        return new Rect(c.x - e.tam.x * 0.5f, c.y - e.tam.y * 0.5f, e.tam.x, e.tam.y);
    }

    void Remover(List<Ente> lista, int i)
    {
        if (lista[i].t != null) Destroy(lista[i].t.gameObject);
        lista.RemoveAt(i);
    }

    void Pontua(int n)
    {
        pontos += n * (turbo > 0f ? 2 : 1);
    }

    void Aviso(string s)
    {
        aviso = s;
        tAviso = 2.5f;
    }

    static Color CorFruta(string nome)
    {
        switch (nome)
        {
            case "Avocado": return new Color(0.4f, 0.6f, 0.2f);
            case "Banana": return new Color(1f, 0.9f, 0.2f);
            case "Lemon": return new Color(0.95f, 1f, 0.3f);
            case "Orange": return new Color(1f, 0.6f, 0.1f);
            case "Pear": return new Color(0.7f, 0.85f, 0.3f);
            case "PineApple": return new Color(1f, 0.75f, 0.1f);
            default: return new Color(0.9f, 0.2f, 0.15f);
        }
    }

    static Sprite Quadrado()
    {
        if (sprQ == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sprQ = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return sprQ;
    }

    static Sprite Circulo()
    {
        if (sprC == null)
        {
            int n = 64;
            Texture2D tex = new Texture2D(n, n);
            tex.filterMode = FilterMode.Bilinear;
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            sprC = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }
        return sprC;
    }

    // ------------------------------------------------------------ TELA (HUD)

    void OnGUI()
    {
        if (cam == null || jogador == null) return;

        int w = Screen.width;
        int h = Screen.height;

        GUIStyle txt = new GUIStyle(GUI.skin.label);
        txt.fontSize = Mathf.Max(14, h / 26);
        txt.normal.textColor = Color.white;

        GUIStyle meio = new GUIStyle(txt);
        meio.alignment = TextAnchor.MiddleCenter;
        meio.wordWrap = true;

        GUIStyle titulo = new GUIStyle(meio);
        titulo.fontSize = Mathf.Max(24, h / 10);
        titulo.fontStyle = FontStyle.Bold;

        if (estado == Estado.Intro)
        {
            Escurecer(w, h, 0.65f);
            GUI.Label(new Rect(0, h * 0.06f, w, h * 0.15f), "FELPUDO'S RACE", titulo);

            string historia =
                "Fofura foi sequestrada por Uruca, o vilão que quer o poder escondido nela!\n" +
                "Felpudo pega o seu carro e parte rumo à montanha. Desvie dos lesmos, colete frutas e resgate a Fofura.\n\n" +
                "OBJETIVO: colete " + frutasParaVencer + " frutas.   DERROTA: perder todas as vidas.\n\n" +
                "Pular / bater asas: ESPAÇO, W, seta para cima ou clique esquerdo\n" +
                "Agachar / mergulhar: S ou seta para baixo\n" +
                "Atirar: F, CTRL esquerdo ou clique direito\n\n" +
                "Lesmo: pule em cima dele ou acerte 2 tiros.   Barril: só 3 tiros.\n" +
                "Abacaxi = escudo   |   Limão = turbo   |   Outras frutas = +1 vida";
            GUI.Label(new Rect(w * 0.1f, h * 0.2f, w * 0.8f, h * 0.65f), historia, meio);
            GUI.Label(new Rect(0, h * 0.86f, w, h * 0.1f), "Aperte ENTER ou clique para começar", meio);
            return;
        }

        // vidas
        float q = h / 18f;
        Texture2D circ = Circulo().texture;
        for (int i = 0; i < vidasMax; i++)
        {
            GUI.color = i < vidas ? new Color(0.95f, 0.1f, 0.2f) : new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(12 + i * (q + 8), 12, q, q), circ);
        }
        GUI.color = Color.white;

        GUI.Label(new Rect(12, 20 + q, w * 0.5f, h * 0.08f),
            "Frutas: " + coletadas + "/" + frutasParaVencer + "    Pontos: " + pontos, txt);

        string status = "";
        if (escudo > 0f) status += "ESCUDO " + Mathf.CeilToInt(escudo) + "s   ";
        if (turbo > 0f) status += "TURBO " + Mathf.CeilToInt(turbo) + "s";
        if (status != "")
            GUI.Label(new Rect(12, 20 + q + h * 0.06f, w * 0.6f, h * 0.08f), status, txt);

        if (tAviso > 0f && estado == Estado.Jogando)
            GUI.Label(new Rect(0, h * 0.08f, w, h * 0.12f), aviso, meio);

        if (estado == Estado.Venceu || estado == Estado.Perdeu)
        {
            Escurecer(w, h, 0.55f);
            bool venceu = estado == Estado.Venceu;
            titulo.normal.textColor = venceu ? Color.white : new Color(1f, 0.35f, 0.35f);
            GUI.Label(new Rect(0, h * 0.2f, w, h * 0.2f),
                venceu ? "VOCÊ RESGATOU A FOFURA!" : "GAME OVER", titulo);
            GUI.Label(new Rect(w * 0.1f, h * 0.45f, w * 0.8f, h * 0.3f),
                (venceu ? "Felpudo chegou à montanha e derrotou os lesmos de Uruca!\n"
                        : "Felpudo ficou sem vidas e a Fofura continua presa...\n")
                + "Frutas: " + coletadas + "/" + frutasParaVencer + "    Pontos: " + pontos
                + "\n\nAperte R para jogar de novo", meio);
        }
    }

    void Escurecer(int w, int h, float a)
    {
        GUI.color = new Color(0f, 0f, 0f, a);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}