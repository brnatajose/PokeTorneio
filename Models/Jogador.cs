public class Jogador
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public int Pontos { get; set; } = 0;
    public int Vitorias { get; set; } = 0;
    public int Derrotas { get; set; } = 0;
    public int Empates { get; set; } = 0;
    public Guid UsuarioId { get; set; }
    public bool TeveBye { get; set; }
    public List<string> ResultadosMelhorDe3 { get; set; } = new List<string>();

    // Nova propriedade para armazenar as partidas
    public ICollection<Partida> Partidas { get; set; } = new List<Partida>();

    // Propriedades OW e OOW
    public double OW { get; private set; } = 0; // Overall Wins (em %)
    public double OOW { get; private set; } = 0; // Opponent's Overall Wins (em %)

    // Métodos para registrar vitórias, empates e derrotas
    public void RegistrarVitoria()
    {
        Vitorias++;
        Pontos += 3;
    }

    public void RegistrarEmpate()
    {
        Empates++;
        Pontos += 1;
    }

    public void RegistrarDerrota()
    {
        Derrotas++;
    }

    // Adicionar resultado Melhor de 3
    public void AdicionarResultadoMelhorDe3(string resultado)
    {
        ResultadosMelhorDe3.Add(resultado);
    }

    // Calcular pontuação Melhor de 3
    public int CalcularPontuacaoMelhorDe3()
    {
        int totalPontos = 0;

        foreach (var resultado in ResultadosMelhorDe3)
        {
            if (resultado == "2x0")
                totalPontos += 3;
            else if (resultado == "2x1")
                totalPontos += 2;
            else if (resultado == "1x0")
                totalPontos += 1;
            else totalPontos += 0;
        }

        return totalPontos;
    }

    // Método para calcular OW (Oponente Wins Percentage)
    public void CalcularOW()
    {
        int totalPartidas = Partidas.Count;
        double totalVitoriasOponentes = 0;

        foreach (var partida in Partidas)
        {
            // Identifica o oponente
            var oponente = partida.Jogador1.Id == this.Id ? partida.Jogador2 : partida.Jogador1;

            // Adiciona as vitórias do oponente
            totalVitoriasOponentes += oponente.Vitorias;
        }

        if (totalPartidas > 0)
        {
            OW = (totalVitoriasOponentes / (totalPartidas * 3)) * 100; // Multiplica por 3, pois uma vitória dá 3 pontos
        }
    }

    // Método para calcular OOW (Oponente Oponente Wins Percentage)
    public void CalcularOOW()
    {
        int totalOponentes = 0; // Total de oponentes enfrentados
        double totalVitoriasOponentesDosOponentes = 0; // Total de vitórias dos oponentes dos oponentes

        foreach (var partida in Partidas)
        {
            // Identifica o oponente
            var oponente = partida.Jogador1.Id == this.Id ? partida.Jogador2 : partida.Jogador1;

            // Contar vitórias dos oponentes
            if (oponente != null)
            {
                totalOponentes++;

                // Contar vitórias dos oponentes dos oponentes
                foreach (var partidaOponente in oponente.Partidas)
                {
                    var outroOponente = partidaOponente.Jogador1.Id == oponente.Id ? partidaOponente.Jogador2 : partidaOponente.Jogador1;
                    totalVitoriasOponentesDosOponentes += outroOponente.Vitorias;
                }
            }
        }

        if (totalOponentes > 0)
        {
            OOW = (totalVitoriasOponentesDosOponentes / (totalOponentes * 3)) * 100; // Multiplica por 3, pois uma vitória dá 3 pontos
        }
    }


}
