using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using WebAPI.Exceptions;
using WebAPI.Models;
using WebAPI.Services;

namespace WebAPI.Tests;

[TestClass]
public class SeatsControllerTests
{
    private const string UserId = "user-123";

    [TestMethod]
    public void ReserveSeat_WhenSeatIsAvailable_ReturnsSeat()
    {
        Seat expectedSeat = new Seat { Number = 42, ExamenUserId = UserId };

        // On mock le service pour contrôler exactement ce que ReserveSeat retourne sans utiliser la BD.
        Mock<SeatsService> serviceMock = new Mock<SeatsService>();
        serviceMock.Setup(s => s.ReserveSeat(UserId, 42)).Returns(expectedSeat);

        // On mock seulement la propriété UserId du contrôleur pour éviter de devoir créer un vrai HttpContext.
        Mock<SeatsController> controllerMock = new Mock<SeatsController>(serviceMock.Object) { CallBase = true };
        controllerMock.Setup(c => c.UserId).Returns(UserId);

        ActionResult<Seat> result = controllerMock.Object.ReserveSeat(42);

        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        OkObjectResult okResult = (OkObjectResult)result.Result!;
        Assert.AreSame(expectedSeat, okResult.Value);
    }

    [TestMethod]
    public void ReserveSeat_WhenSeatIsAlreadyTaken_ReturnsUnauthorized()
    {
        // On force le service mocké à lancer la même exception que le vrai service dans ce scénario.
        Mock<SeatsService> serviceMock = new Mock<SeatsService>();
        serviceMock.Setup(s => s.ReserveSeat(UserId, 42)).Throws(new SeatAlreadyTakenException());

        // CallBase permet d'appeler la vraie méthode ReserveSeat du contrôleur, sauf les membres qu'on setup.
        Mock<SeatsController> controllerMock = new Mock<SeatsController>(serviceMock.Object) { CallBase = true };
        controllerMock.Setup(c => c.UserId).Returns(UserId);

        ActionResult<Seat> result = controllerMock.Object.ReserveSeat(42);

        Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public void ReserveSeat_WhenSeatNumberIsTooHigh_ReturnsNotFoundWithMessage()
    {
        const int seatNumber = 101;

        // Le mock simule une place hors limite sans passer par la logique interne du service.
        Mock<SeatsService> serviceMock = new Mock<SeatsService>();
        serviceMock.Setup(s => s.ReserveSeat(UserId, seatNumber)).Throws(new SeatOutOfBoundsException());

        Mock<SeatsController> controllerMock = new Mock<SeatsController>(serviceMock.Object) { CallBase = true };
        controllerMock.Setup(c => c.UserId).Returns(UserId);

        ActionResult<Seat> result = controllerMock.Object.ReserveSeat(seatNumber);

        Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        NotFoundObjectResult notFoundResult = (NotFoundObjectResult)result.Result!;
        Assert.AreEqual("Could not find" + seatNumber, notFoundResult.Value);
    }

    [TestMethod]
    public void ReserveSeat_WhenUserAlreadyHasASeat_ReturnsBadRequest()
    {
        // Cette exception représente le cas où l'utilisateur connecté possède déjà une réservation.
        Mock<SeatsService> serviceMock = new Mock<SeatsService>();
        serviceMock.Setup(s => s.ReserveSeat(UserId, 42)).Throws(new UserAlreadySeatedException());

        Mock<SeatsController> controllerMock = new Mock<SeatsController>(serviceMock.Object) { CallBase = true };
        controllerMock.Setup(c => c.UserId).Returns(UserId);

        ActionResult<Seat> result = controllerMock.Object.ReserveSeat(42);

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestResult));
    }
}
