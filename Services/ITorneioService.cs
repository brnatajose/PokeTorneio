using PokeTorneio.Enums;
using PokeTorneio.Models;
using System.Collections.Generic;

namespace PokeTorneio.Services
{
    public interface ITorneioService
    {
        // Métodos para Gerenciamento de Torneios
        IEnumerable<Torneio> ListarTorneios();
        Torneio ObterTorneioPorId(int torneioId);
        void AdicionarTorneio(Torneio torneio);
        void FinalizarTorneio(int torneioId);
        void SalvarPartida(Partida partida);

        // Métodos para Gerenciamento de Jogadores
        void AdicionarJogadores(int torneioId, IEnumerable<Jogador> jogadores);

        // Métodos para Gerenciamento de Rodadas
        Rodada IniciarRodada(int torneioId);
        Rodada ObterPorRodadaId(int rodadaId);

        // Métodos para Gerenciamento de Resultados
        void RegistrarResultado(int partidaId, ResultadoMelhorDe3 resultadoMelhorDe3, int resultado, Guid vencedorId);

        // Métodos para Obtenção de Dados
        Torneio ObterTorneioPorPartida(int partidaId);
        Partida ObterPartidaPorId(int partidaId);

        // Cálculo
        int CalcularNumeroDeRodadas(int numeroDeJogadores);
        void SalvarAlteracoesRodada(int rodadaId, List<Partida> partidas);  
        void EditarNomeJogador(Guid jogadorId, string novoNome);
        void RemoverJogador(int torneioId, Guid jogadorId);
        void ExcluirTorneio(int torneioId);


    }
}
