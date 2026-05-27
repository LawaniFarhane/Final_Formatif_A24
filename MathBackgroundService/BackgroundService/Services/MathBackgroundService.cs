using BackgroundServiceMath.Data;
using BackgroundServiceMath.Models;
using BackgroundServiceVote.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackgroundServiceMath.Services;

public class UserData
{
    public int Choice { get; set; } = -1;
    public int NbConnections { get; set; } = 0;
}

public class MathBackgroundService : BackgroundService
{
    public const int DELAY = 20 * 1000;

    private Dictionary<string, UserData> _data = new();

    private IHubContext<MathQuestionsHub> _mathQuestionHub;

    private MathQuestion? _currentQuestion;

    public MathQuestion? CurrentQuestion => _currentQuestion;

    private MathQuestionsService _mathQuestionsService;

    private IServiceScopeFactory _scopeFactory;

    public MathBackgroundService(IHubContext<MathQuestionsHub> mathQuestionHub, MathQuestionsService mathQuestionsService, IServiceScopeFactory scopeFactory)
    {
        _mathQuestionHub = mathQuestionHub;
        _mathQuestionsService = mathQuestionsService;
        // Le BackgroundService est un singleton, donc on garde le ScopeFactory pour créer un scope seulement quand on a besoin de la BD.
        _scopeFactory = scopeFactory;
    }

    public void AddUser(string userId)
    {
        if (!_data.ContainsKey(userId))
        { 
            _data[userId] = new UserData();
        }
        _data[userId].NbConnections++;
    }

    public void RemoveUser(string userId)
    {
        if (!_data.ContainsKey(userId))
        {
            _data[userId].NbConnections--;
            if(_data[userId].NbConnections <= 0)
                _data.Remove(userId);
        }
    }

    public async Task SelectChoice(string userId, int choice)
    {
        if (_currentQuestion == null)
            return;

        UserData userData = _data[userId];
            
        if (userData.Choice != -1)
            throw new Exception("A user cannot change is choice!");

        userData.Choice = choice;

        _currentQuestion.PlayerChoices[choice]++;

        // TODO: Notifier les clients qu'un joueur a choisi une réponse
        // On avise tous les clients du choix reçu pour que les badges se mettent à jour sans attendre la prochaine question.
        await _mathQuestionHub.Clients.All.SendAsync("IncreasePlayersChoices", choice);
    }

    private async Task EvaluateChoices()
    {
        // TODO: La méthode va avoir besoin d'un scope
        // On crée un scope parce que BackgroundServiceContext est scoped et ne peut pas être injecté directement dans ce singleton.
        using var scope = _scopeFactory.CreateScope();
        // On récupère un DbContext dans ce scope pour modifier les joueurs pendant l'évaluation des réponses.
        BackgroundServiceContext context = scope.ServiceProvider.GetRequiredService<BackgroundServiceContext>();

        foreach (var userId in _data.Keys)
        {
            var userData = _data[userId];
            // TODO: Notifier les clients pour les bonnes et mauvaises réponses
            // TODO: Modifier et sauvegarder le NbRightAnswers des joueurs qui ont la bonne réponse
            if (userData.Choice == _currentQuestion!.RightAnswerIndex)
            {
                // Le client connecté avec ce userId reçoit seulement son propre résultat.
                await _mathQuestionHub.Clients.User(userId).SendAsync("RightAnswer");

                // On met aussi la BD à jour pour que le score reste correct après un refresh de la page.
                Player player = await context.Player.SingleAsync(p => p.UserId == userId);
                player.NbRightAnswers++;
            }
            else
            {
                // On envoie la valeur de la bonne réponse pour que le client puisse l'afficher dans son alert.
                await _mathQuestionHub.Clients.User(userId).SendAsync("WrongAnswer", _currentQuestion.Answers[_currentQuestion.RightAnswerIndex]);
            }

        }
        // On sauvegarde une seule fois après avoir évalué tous les joueurs qui avaient la bonne réponse.
        await context.SaveChangesAsync();

        // Reset
        foreach (var key in _data.Keys)
        {
            _data[key].Choice = -1;
        }
    }

    private async Task Update(CancellationToken stoppingToken)
    {
        if (_currentQuestion != null)
        {
            await EvaluateChoices();
        }

        _currentQuestion = _mathQuestionsService.CreateQuestion();

        await _mathQuestionHub.Clients.All.SendAsync("CurrentQuestion", _currentQuestion);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Update(stoppingToken);
            await Task.Delay(DELAY, stoppingToken);
        }
    }
}
