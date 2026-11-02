Felpudo’s Race: O resgate da Fofura ([https://play.unity.com/en/games/cf1e034b-e44b-4b4c-9b62-3167a7becab4/felpudo-race-trablho])

Jogo 2D de corrida e ação feito na Unity 6 com sprites, paralaxe e sistema de partículas. O Felpudo pega o seu carro e corre pela cidade para resgatar a Fofura, sequestrada pelo vilão Uruca. No caminho, desvia de lesmos e barris, atira, coleta frutas e usa poderes temporários para chegar à montanha.

Objetivo

Coletar 15 frutas para resgatar a Fofura (condição de vitória).
Não perder as 3 vidas (condição de derrota).
Desviar ou destruir os inimigos que aparecem pela estrada.
Fazer o máximo de pontos.

Como jogar

Ação	Teclas
Pular	Espaço, W, seta para cima ou clique esquerdo
Agachar e mergulhar	S ou seta para baixo
Atirar	F, Ctrl esquerdo ou clique direito
Jogar de novo	R ou Enter

Na tela inicial, aperte Enter ou clique para começar.

Elementos do jogo e descrição 

Felpudo	Herói que dirige o carro, pula, agacha e atira
Lesmo	Inimigo lento. Morre pisando em cima ou com 2 tiros (20 pontos)
Barril	Inimigo rápido. Pisar não adianta, só 3 tiros o destroem (30 pontos)
Frutas	Abacate, banana, pera e tomate dão +1 vida. Abacaxi dá escudo (6 s). Limão dá turbo (5 s), com mais velocidade e pontos em dobro
Cenário	Fundo em camadas com paralaxe: céu, nuvens, névoa, prédios, casas, muro e chão
Interface	Vidas, frutas coletadas, pontos e avisos de poderes
Efeitos	Partículas ao acertar, coletar e levar dano

Regras resumidas

O carro fica parado na horizontal e o cenário rola da direita para a esquerda.
Tocar em um inimigo tira 1 vida, seguida de 1,5 s de invencibilidade. O escudo bloqueia o dano.
A frequência dos inimigos aumenta conforme você coleta frutas.
Ao coletar 15 frutas, aparece a tela de vitória. Com 0 vidas, aparece GAME OVER.

Como foi feito

Engine: Unity 6 , template 2D
Linguagem: C#, com um script único (FelpudoRace.cs) que controla fundo, jogador, inimigos, frutas, tiros e telas
Efeitos: animação de sprites, partículas e paralaxe
Interface: tela montada com OnGUI
Publicação: build WebGL no PlayUnity

Demonstração
O vídeo ([https://youtu.be/u_z6nin5XYU ]) mostra uma partida completa, da tela inicial até a vitória.


Créditos
Trabalho desenvolvido por [Sostenes Henrique].
Sprites do Felpudo, dos inimigos, das frutas e dos cenários: material da prática [Lab 10] (UNEB).