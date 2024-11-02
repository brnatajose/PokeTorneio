using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeTorneio.Data;
using PokeTorneio.Enums;
using PokeTorneio.Models;
using System.Collections.Generic;
using System.Linq;

namespace PokeTorneio.Services
{
    public class TorneioService : ITorneioService
    {
        private readonly ApplicationDbContext _context;

        public TorneioService(ApplicationDbContext context)
        {
            _context = context;
        }
        public void ExcluirTorneio(int torneioId)
        {
            var torneio = _context.Torneios.Find(torneioId);
            if (torneio != null)
            {
                _context.Torneios.Remove(torneio);
                _context.SaveChanges();
            }
            else
            {
                throw new Exception("Torneio não encontrado.");
            }
        }
        public void RemoverJogador(int torneioId, Guid jogadorId)
        {
            var torneio = _context.Torneios.Include(t => t.Jogadores).FirstOrDefault(t => t.Id == torneioId);
            if (torneio == null)
            {
                throw new Exception("Torneio não encontrado.");
            }

            var jogador = torneio.Jogadores.FirstOrDefault(j => j.Id == jogadorId);
            if (jogador == null)
            {
                throw new Exception("Jogador não encontrado.");
            }

            torneio.Jogadores.Remove(jogador);
            _context.SaveChanges();
        }

        // Listagem de Torneios
        public IEnumerable<Torneio> ListarTorneios() => _context.Torneios.ToList();

        public int CalcularNumeroDeRodadas(int numeroDeJogadores)
        {
            return numeroDeJogadores < 2 ? 0 : (int)Math.Ceiling(Math.Log(numeroDeJogadores, 2));
        }

        // Obtenção de Torneios
        public Torneio ObterTorneioPorId(int torneioId) =>
            _context.Torneios
                .Include(t => t.Jogadores)
                .Include(t => t.Rodadas)
                    .ThenInclude(r => r.Partidas)
                .FirstOrDefault(t => t.Id == torneioId);

        public Torneio ObterTorneioPorPartida(int partidaId) =>
            _context.Torneios
                .FirstOrDefault(t => t.Rodadas.Any(r => r.Partidas.Any(p => p.Id == partidaId)));

        // Adição de Torneios e Jogadores
        public void AdicionarTorneio(Torneio torneio)
        {
            _context.Torneios.Add(torneio);
            _context.SaveChanges();
        }

        public void AdicionarJogadores(int torneioId, IEnumerable<Jogador> jogadores)
        {
            // Obter o torneio a partir do ID
            var torneio = _context.Torneios
                .Include(t => t.Jogadores) // Inclui a lista de jogadores para evitar problemas de referência
                .FirstOrDefault(t => t.Id == torneioId);

            if (torneio != null)
            {
                // Adiciona os jogadores ao torneio
                foreach (var jogador in jogadores)
                {
                    torneio.Jogadores.Add(jogador);
                }
                _context.SaveChanges(); // Salva as mudanças no contexto
            }
        }

        public void FinalizarTorneio(int torneioId)
        {
            var torneio = ObterTorneioPorId(torneioId);
            if (torneio != null)
            {
                torneio.IsFinalizado = true;

                foreach (var jogador in torneio.Jogadores)
                {
                    jogador.CalcularOW(); 

                    jogador.CalcularOOW();
                }

                _context.SaveChanges(); // Salva as alterações no banco de dados
            }
        }


        // Lógica de Vencedor
        public Jogador CalcularVencedor(Torneio torneio)
        {
            var jogadoresComVitorias = torneio.Jogadores
                .Where(j => j.Vitorias > 0)
                .OrderByDescending(j => j.Vitorias)
                .ThenByDescending(j => j.CalcularPontuacaoMelhorDe3())
                .ToList();

            if (jogadoresComVitorias.Count > 1 && jogadoresComVitorias[0].Vitorias == jogadoresComVitorias[1].Vitorias)
            {
                return CompararDesempenho(jogadoresComVitorias[0], jogadoresComVitorias[1], torneio);
            }

            return jogadoresComVitorias.FirstOrDefault();
        }

        private Jogador CompararDesempenho(Jogador j1, Jogador j2, Torneio torneio)
        {
            var oponentWinJ1 = CalcularOponentWin(j1, torneio);
            var oponentWinJ2 = CalcularOponentWin(j2, torneio);

            if (oponentWinJ1 > oponentWinJ2)
                return j1;
            else if (oponentWinJ2 > oponentWinJ1)
                return j2;

            var oponentOponentWinJ1 = CalcularOponentOponentWin(j1, torneio);
            var oponentOponentWinJ2 = CalcularOponentOponentWin(j2, torneio);

            return oponentOponentWinJ1 > oponentOponentWinJ2 ? j1 : j2;
        }
        private int CalcularOponentWin(Jogador jogador, Torneio torneio)
        {
            var partidasVencidas = torneio.Rodadas
                .SelectMany(r => r.Partidas)
                .Where(p => p.VencedorId == jogador.Id)
                .ToList();

            return partidasVencidas.Sum(p => p.Jogador1Id == jogador.Id ? p.Jogador2.Pontos : p.Jogador1.Pontos);
        }

        private int CalcularOponentOponentWin(Jogador jogador, Torneio torneio)
        {
            var partidasJogadas = torneio.Rodadas
                .SelectMany(r => r.Partidas)
                .Where(p => p.Jogador1Id == jogador.Id || p.Jogador2Id == jogador.Id)
                .ToList();

            int oponentOponentWin = 0;

            foreach (var partida in partidasJogadas)
            {
                var oponente = partida.Jogador1Id == jogador.Id ? partida.Jogador2 : partida.Jogador1;

                oponentOponentWin += torneio.Rodadas
                    .SelectMany(r => r.Partidas)
                    .Where(p => p.Jogador1Id == oponente.Id || p.Jogador2Id == oponente.Id)
                    .Sum(p => p.Jogador1Id == oponente.Id ? p.Jogador2.Pontos : p.Jogador1.Pontos);
            }

            return oponentOponentWin;
        }


        private Jogador CompararDesempenho(Jogador j1, Jogador j2)
        {
            return CalcularPontuacao(j1) > CalcularPontuacao(j2) ? j1 : j2;
        }

        private int CalcularPontuacao(Jogador jogador) =>
            jogador.Partidas.Sum(p => CalcularResultadoPartida(p));

        public void SalvarPartida(Partida partida)
        {
            var partidaExistente = _context.Partidas.Find(partida.Id);

            if (partidaExistente != null)
            {
                partidaExistente.Resultado = partida.Resultado;
                partidaExistente.ResultadoMelhorDe3 = partida.ResultadoMelhorDe3;
                AtualizarResultadoJogadores(partidaExistente, partida.Resultado);
                _context.Partidas.Update(partidaExistente);
            }
            else
            {
                partida.Jogador1.Partidas.Add(partida);
                partida.Jogador2.Partidas.Add(partida);
                _context.Partidas.Add(partida);
            }

            _context.SaveChanges();
        }

        private int CalcularResultadoPartida(Partida partida) =>
            partida.ResultadoMelhorDe3 switch
            {
                Enums.ResultadoMelhorDe3.DoisAZero => 3,
                Enums.ResultadoMelhorDe3.DoisAUm => 2,
                Enums.ResultadoMelhorDe3.UmAZero => 1,
                _ => 0,
            };

        public void RegistrarResultado(int partidaId, ResultadoMelhorDe3 resultadoMelhorDe3, int resultado, Guid vencedorId)
        {
            Partida partida = _context.Partidas.Find(partidaId);
            if (partida == null) return;

            partida.Resultado = resultado;
            partida.ResultadoMelhorDe3 = resultadoMelhorDe3;

            if (resultado == 0) // Empate
            {
                partida.Jogador1.RegistrarEmpate();
                partida.Jogador2.RegistrarEmpate();
                partida.VencedorId = null;
            }
            else if (resultado == 1) // Jogador 1 vence
            {
                partida.Jogador1.AdicionarResultadoMelhorDe3(EnumHelper.GetDescription(resultadoMelhorDe3));
                partida.Jogador1.RegistrarVitoria();
                partida.Jogador1.Pontos = partida.Jogador1.Pontos + partida.Jogador1.CalcularPontuacaoMelhorDe3();

                partida.Jogador2.RegistrarDerrota();
            }
            else if (resultado == 2) // Jogador 2 vence
            {
                partida.Jogador2.AdicionarResultadoMelhorDe3(EnumHelper.GetDescription(resultadoMelhorDe3));
                partida.Jogador2.RegistrarVitoria();
                partida.Jogador2.Pontos = partida.Jogador2.Pontos + partida.Jogador2.CalcularPontuacaoMelhorDe3();

                partida.Jogador1.RegistrarDerrota();
            }

            if (resultado != 0)
            {
                partida.VencedorId = vencedorId;
            }
            _context.SaveChanges();
        }



        // Método para iniciar a rodada
        // Método para iniciar a rodada
        public Rodada IniciarRodada(int torneioId)
        {
            var torneio = ObterTorneioPorId(torneioId);
            if (torneio == null || !torneio.Jogadores.Any()) return null;

            var rodada = new Rodada
            {
                NumeroRodada = torneio.Rodadas.Count + 1,
                TorneioId = torneioId
            };

            // Verifica quantas rodadas devem ser criadas com base no número de jogadores
            int numRodadas = CalcularNumeroDeRodadas(torneio.Jogadores.Count);

            // Antes de criar as partidas, calcula OW e OOW se não for a primeira rodada
            if (rodada.NumeroRodada > 1)
            {
                foreach (var jogador in torneio.Jogadores)
                {
                    jogador.CalcularOW();
                    jogador.CalcularOOW();
                }
                _context.SaveChanges(); // Salva as alterações após calcular OW e OOW
            }

            CriarPartidas(rodada, torneio);
            torneio.AdicionarRodada(rodada);
            _context.SaveChanges();
            return rodada;
        }

        private void CriarPartidas(Rodada rodada, Torneio torneio)
        {
            var jogadores = torneio.Jogadores.ToList();
            var partidas = new List<Partida>();
            var jogadoresBye = new List<Jogador>();

            // Verifica se é a primeira rodada
            if (rodada.NumeroRodada == 1)
            {
                // Lógica para criar as partidas da primeira rodada (sem OW e OOW)
                for (int i = 0; i < jogadores.Count; i += 2)
                {
                    if (i + 1 < jogadores.Count)
                    {
                        var partida = new Partida
                        {
                            Jogador1Id = jogadores[i].Id,
                            Jogador2Id = jogadores[i + 1].Id,
                            RodadaId = rodada.Id,
                            TorneioId = rodada.TorneioId,
                            Resultado = null,
                            ResultadoMelhorDe3 = null,
                            VencedorId = null
                        };
                        partidas.Add(partida);
                    }
                    else
                    {
                        jogadoresBye.Add(jogadores[i]);
                    }
                }

                // Gerenciar bye
                if (jogadoresBye.Count > 0)
                {
                    var jogadorBye = jogadoresBye[new Random().Next(jogadoresBye.Count)];
                    jogadorBye.RegistrarVitoria();
                    jogadorBye.TeveBye = true;
                }

                rodada.Partidas.AddRange(partidas);
                return;
            }

            // Lógica para rodadas subsequentes
            var jogadoresEmparelhados = new HashSet<Guid>();
            var partidasAnteriores = _context.Partidas.Where(t => t.TorneioId == torneio.Id).ToList();

            // Ordena jogadores por pontos e "teve bye"
            jogadores = jogadores.OrderByDescending(j => j.TeveBye).ThenByDescending(j => j.Pontos).ToList();           

            // Emparelhamento de jogadores
            for (int i = 0; i < jogadores.Count; i++)
            {
                var jogador1 = jogadores[i];

                if (jogadoresEmparelhados.Contains(jogador1.Id))
                    continue;

                bool emparelhado = false;

                for (int j = i + 1; j < jogadores.Count; j++)
                {
                    var jogador2 = jogadores[j];

                    if (jogadoresEmparelhados.Contains(jogador2.Id) || jogador1.Id == jogador2.Id) continue;

                    if (!JaSeEnfrentaram(jogador1, jogador2, partidasAnteriores))
                    {
                        var partida = new Partida
                        {
                            Jogador1Id = jogador1.Id,
                            Jogador2Id = jogador2.Id,
                            RodadaId = rodada.Id,
                            TorneioId = torneio.Id,
                            Resultado = null,
                            ResultadoMelhorDe3 = null,
                            VencedorId = null
                        };

                        partidas.Add(partida);
                        jogadoresEmparelhados.Add(jogador1.Id);
                        jogadoresEmparelhados.Add(jogador2.Id);
                        emparelhado = true;
                        break;
                    }
                }

                if (!emparelhado && jogadores.Count % 2 != 0)
                {
                    jogadoresBye.Add(jogador1);
                }
            }

            if (jogadoresBye.Count > 0)
            {
                var jogadorBye = jogadoresBye.First();
                jogadorBye.RegistrarVitoria();
                jogadorBye.TeveBye = true;
            }

            rodada.Partidas.AddRange(partidas);
        }


        // Método para verificar se dois jogadores já se enfrentaram
        private bool JaSeEnfrentaram(Jogador jogador1, Jogador jogador2, List<Partida> partidas)
        {
            return partidas.Any(p => (p.Jogador1Id == jogador1.Id && p.Jogador2Id == jogador2.Id) ||
                                      (p.Jogador1Id == jogador2.Id && p.Jogador2Id == jogador1.Id));
        }






        // Método para obter uma rodada por ID
        public Rodada ObterPorRodadaId(int rodadaId) =>
            _context.Rodadas
                .Include(r => r.Partidas)
                    .ThenInclude(p => p.Jogador1)
                .Include(r => r.Partidas)
                    .ThenInclude(p => p.Jogador2)
                .FirstOrDefault(r => r.Id == rodadaId);

        public Partida ObterPartidaPorId(int partidaId) =>
            _context.Partidas
                .Include(p => p.Jogador1)
                .Include(p => p.Jogador2)
                .FirstOrDefault(p => p.Id == partidaId);

        public void AtualizarResultadoJogadores(Partida partida, int? resultado)
        {
            AtualizarJogadores(partida, resultado);
            _context.SaveChanges();
        }

        private void AtualizarJogadores(Partida partida, int? resultado)
        {
            switch (resultado)
            {
                case 1:
                    partida.Jogador1.RegistrarVitoria();
                    partida.Jogador2.RegistrarDerrota();
                    break;
                case 2:
                    partida.Jogador2.RegistrarVitoria();
                    partida.Jogador1.RegistrarDerrota();
                    break;
                case 0:
                    partida.Jogador1.RegistrarEmpate();
                    partida.Jogador2.RegistrarEmpate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resultado), "Resultado inválido.");
            }
        }

        public void SalvarAlteracoesRodada(int rodadaId, List<Partida> partidas)
        {
            var rodada = ObterPorRodadaId(rodadaId);

            if (rodada != null)
            {
                foreach (var partida in partidas)
                {
                    partida.RodadaId = rodadaId;
                    partida.TorneioId = rodada.TorneioId;

                    var partidaExistente = rodada.Partidas.FirstOrDefault(p => p.Id == partida.Id);
                    
                        partidaExistente.Jogador1Id = partida.Jogador1Id;
                        partidaExistente.Jogador2Id = partida.Jogador2Id;                    
                }

                _context.SaveChanges();
            }
        }

        public void EditarNomeJogador(Guid jogadorId, string novoNome)
        {
            var jogador = _context.Jogadores.Find(jogadorId);
            if (jogador == null)
            {
                throw new Exception("Jogador não encontrado");
            }

            jogador.Nome = novoNome;
            _context.SaveChanges();
        }

    }
}