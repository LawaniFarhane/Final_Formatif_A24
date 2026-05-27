using BackgroundServiceMath.Data;
using BackgroundServiceMath.DTOs;
using BackgroundServiceMath.Models;
using BackgroundServiceMath.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackgroundServiceVote.Hubs;

[Authorize]
public class MathQuestionsHub : Hub
{
    private MathBackgroundService _matchBackgroundService;
    private BackgroundServiceContext _backgroundServiceContext;

    public MathQuestionsHub(MathBackgroundService matchBackgroundService, BackgroundServiceContext backgroundServiceContext)
    {
        _matchBackgroundService = matchBackgroundService;
        _backgroundServiceContext = backgroundServiceContext;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        _matchBackgroundService.AddUser(Context.UserIdentifier!);

        Player player = _backgroundServiceContext.Player.Where(p => p.UserId == Context.UserIdentifier!).Single();

        await Clients.Caller.SendAsync("PlayerInfo", new PlayerInfoDTO()
        {
            NbRightAnswers = player.NbRightAnswers,
        });

        await Clients.Caller.SendAsync("CurrentQuestion", _matchBackgroundService.CurrentQuestion);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _matchBackgroundService.RemoveUser(Context.UserIdentifier!);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SelectChoice(int answerIndex)
    {
        // On attend que le BackgroundService ait envoyé le message SignalR avant de terminer l'appel du client.
        await _matchBackgroundService.SelectChoice(Context.UserIdentifier!, answerIndex);
    }
}