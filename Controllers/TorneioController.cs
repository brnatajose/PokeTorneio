using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeTorneio.Enums;
using PokeTorneio.Models;
using PokeTorneio.Services;
using System.Collections.Generic;
using System.Linq;

namespace PokeTorneio.Controllers
{
    public class TorneioController : Controller
    {
        private readonly ITorneioService _torneioService;

        public TorneioController(ITorneioService torneioService)
        {
            _torneioService = torneioService;
        }

        // GET: /Torneio/Index
        public IActionResult Index()
        {
            var torneios = _torneioService.ListarTorneios();
            return View(torneios);
        }

        // GET: /Torneio/AdicionarJogadores/5
        public IActionResult AdicionarJogadores(int id)
        {
            var torneio = _torneioService.ObterTorneioPorId(id);
            return torneio == null ? NotFound() : View(torneio);
        }

        // POST: /Torneio/SalvarJogadores/5
        [HttpPost]
        public IActionResult SalvarJogadores(int id, List<Jogador> jogadores)
        {
            var torneio = _torneioService.ObterTorneioPorId(id);
            if (torneio == null)
            {
                return NotFound();
            }

            _torneioService.AdicionarJogadores(id, jogadores);
            return RedirectToAction("Detalhes", new { id });
        }

        // GET: /Torneio/Detalhes/5
        public IActionResult Detalhes(int id)
        {
            var torneio = _torneioService.ObterTorneioPorId(id);
            if (torneio == null)
            {
                return NotFound();
            }

            ViewBag.NumeroDeRodadas = _torneioService.CalcularNumeroDeRodadas(torneio.Jogadores.Count);
            ViewBag.ResultadosMelhorDe3 = EnumHelper.GetEnumSelectList<ResultadoMelhorDe3>();

            return View(torneio);
        }
        public IActionResult EditarRodada(int rodadaId)
        {
            var rodada = _torneioService.ObterPorRodadaId(rodadaId);
            if (rodada == null)
            {
                return NotFound();
            }           

            return View(rodada); 
        }

        [HttpPost]
        public IActionResult SalvarAlteracoesRodada(int rodadaId, List<Partida> partidas)
        {
            var rodada = _torneioService.ObterPorRodadaId(rodadaId);

            if (rodada != null && partidas != null && partidas.Any())
            {
                _torneioService.SalvarAlteracoesRodada(rodadaId, partidas);
            }

            return RedirectToAction("Detalhes", new { id = rodada.TorneioId });
        }





        // POST: /Torneio/Finalizar/5
        [HttpPost]
        public IActionResult FinalizarTorneio(int id)
        {
            _torneioService.FinalizarTorneio(id);
            return RedirectToAction("Detalhes", new { id });
        }
        
        // GET: /Torneio/Criar
        public IActionResult Criar()
        {
            ViewBag.ResultadosMelhorDe3 = EnumHelper.GetEnumSelectList<ResultadoMelhorDe3>();

            return View(new Torneio());
        }
        [HttpPost]
        public IActionResult RemoverJogador(Guid id, int torneioId)
        {
            try
            {
                _torneioService.RemoverJogador(torneioId, id);
                TempData["SuccessMessage"] = "Jogador removido com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Detalhes", new { id = torneioId });
        }

        [HttpPost]
        public IActionResult EditarJogador(Guid jogadorId, string nome, int id)
        {
            try
            {
                _torneioService.EditarNomeJogador(jogadorId, nome);
                // Sucesso ao editar nome
                return RedirectToAction("Detalhes", new { id });
            }
            catch (Exception ex)
            {
                // Lida com o erro e apresenta feedback ao usuário
                TempData["Erro"] = ex.Message;
                return RedirectToAction("Detalhes", new { id });
            }
        }

        // POST: /Torneio/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(Torneio torneio)
        {
            if (ModelState.IsValid)
            {
                _torneioService.AdicionarTorneio(torneio);
                return RedirectToAction("Index");
            }
            return View(torneio);
        }

        // POST: /Torneio/IniciarRodada/5
        [HttpPost]
        public IActionResult IniciarRodada(int id)
        {
            var rodada = _torneioService.IniciarRodada(id);
            return rodada == null ? BadRequest("Erro ao iniciar a rodada.") : RedirectToAction("Detalhes", new { id });
        }

        // POST: /Torneio/RegistrarResultado
        [HttpPost]
        public IActionResult RegistrarResultado(int partidaId, ResultadoMelhorDe3 resultadoMelhorDe3, Guid vencedorId)
        {
            // Validação dos parâmetros
            if (partidaId <= 0)
            {
                ModelState.AddModelError("partidaId", "ID da partida inválido.");
                return BadRequest(ModelState);
            }

            if (!Enum.IsDefined(typeof(ResultadoMelhorDe3), resultadoMelhorDe3))
            {
                ModelState.AddModelError("resultadoMelhorDe3", "Resultado inválido.");
                return BadRequest(ModelState);
            }

            // Obter a partida
            var partida = _torneioService.ObterPartidaPorId(partidaId);
            if (partida == null)
            {
                return NotFound();
            }

            // Definir o resultado com base no vencedorId
            int resultado;
            if (vencedorId == Guid.Empty)
            {
                resultado = 0; // Empate
            }
            else if (vencedorId == partida.Jogador1Id)
            {
                resultado = 1; // Jogador 1 venceu
            }
            else if (vencedorId == partida.Jogador2Id)
            {
                resultado = 2; // Jogador 2 venceu
            }
            else
            {
                ModelState.AddModelError("vencedorId", "Vencedor inválido.");
                return BadRequest(ModelState);
            }

            // Registrar o resultado
            try
            {
                _torneioService.RegistrarResultado(partidaId, resultadoMelhorDe3, resultado, vencedorId);
            }
            catch (Exception ex)
            {
                // Log do erro (substitua pelo seu mecanismo de logging)
                // _logger.LogError(ex, "Erro ao registrar resultado da partida {PartidaId}", partidaId);
                return StatusCode(500, "Erro ao registrar o resultado. Tente novamente mais tarde.");
            }

            // Redirecionar para os detalhes da partida
            return RedirectToAction("Detalhes", new { id = partida.TorneioId });
        }



    }
}
