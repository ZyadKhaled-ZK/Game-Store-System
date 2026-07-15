using FluentAssertions;
using GameStore.DAL.Repo;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Services;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class SupportTicketsControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_Tickets()
    {
        var svc = new Mock<ISupportTicketService>();
        svc.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync([
            new SupportTicket { Id = "t1", UserId = "u1", Subject = "Help", Message = "M", Status = TicketStatus.Open }
        ]);
        svc.Setup(s => s.GetCountAsync()).ReturnsAsync(1);

        var controller = new SupportTicketsController(svc.Object, Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller);

        var result = await controller.Index(1);

        var view = Assert.IsType<ViewResult>(result);
        var tickets = Assert.IsAssignableFrom<List<SupportTicket>>(view.Model);
        tickets.Should().HaveCount(1);
    }

    [Fact]
    public async Task Details_Returns_View_When_Ticket_Found()
    {
        var svc = new Mock<ISupportTicketService>();
        svc.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync(
            new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });

        var controller = new SupportTicketsController(svc.Object, Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("t1");

        var view = Assert.IsType<ViewResult>(result);
        var ticket = Assert.IsType<SupportTicket>(view.Model);
        ticket.Subject.Should().Be("S");
    }

    [Fact]
    public async Task Details_Redirects_When_Not_Found()
    {
        var svc = new Mock<ISupportTicketService>();
        svc.Setup(s => s.GetByIdAsync("nonexistent")).ReturnsAsync((SupportTicket?)null);

        var controller = new SupportTicketsController(svc.Object, Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("nonexistent");

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Details_Redirects_When_Id_Empty()
    {
        var controller = new SupportTicketsController(Mock.Of<ISupportTicketService>(),
            Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("");

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Reply_Redirects_With_Success()
    {
        var svc = new Mock<ISupportTicketService>();
        svc.Setup(s => s.AddReplyAsync("t1", "admin", "Thanks")).ReturnsAsync(
            new SupportTicketReply { Id = "r1", TicketId = "t1", Message = "Thanks" });
        svc.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync(
            new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });

        var controller = new SupportTicketsController(svc.Object, Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller, userId: "admin");

        var result = await controller.Reply("t1", "Thanks");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Details");
        controller.TempData["Message"].Should().Be("Reply posted.");
    }

    [Fact]
    public async Task Reply_Redirects_With_Error_When_Empty()
    {
        var controller = new SupportTicketsController(Mock.Of<ISupportTicketService>(),
            Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller);

        var result = await controller.Reply("t1", "");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Message is required.");
        controller.TempData["IsError"].Should().Be(true);
    }

    [Fact]
    public async Task UpdateStatus_Updates_And_Redirects()
    {
        var svc = new Mock<ISupportTicketService>();
        svc.Setup(s => s.UpdateStatusAsync("t1", TicketStatus.Resolved)).Returns(Task.CompletedTask);
        svc.Setup(s => s.GetByIdAsync("t1")).ReturnsAsync(
            new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });

        var controller = new SupportTicketsController(svc.Object, Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller);

        var result = await controller.UpdateStatus("t1", TicketStatus.Resolved);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Details");
    }

    [Fact]
    public async Task Delete_Redirects_With_Success()
    {
        var uow = new Mock<IUnitOfWork>();
        var repo = new Mock<IRepository<SupportTicket>>();
        repo.Setup(r => r.GetByIdAsync("t1")).ReturnsAsync(
            new SupportTicket { Id = "t1", UserId = "u1", Subject = "S", Message = "M", Status = TicketStatus.Open });
        uow.Setup(u => u.Repository<SupportTicket>()).Returns(repo.Object);

        var controller = new SupportTicketsController(Mock.Of<ISupportTicketService>(),
            Mock.Of<INotificationService>(), uow.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Delete("t1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Ticket deleted.");
    }

    [Fact]
    public async Task Delete_Redirects_With_Error_When_Not_Found()
    {
        var uow = new Mock<IUnitOfWork>();
        var repo = new Mock<IRepository<SupportTicket>>();
        repo.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((SupportTicket?)null);
        uow.Setup(u => u.Repository<SupportTicket>()).Returns(repo.Object);

        var controller = new SupportTicketsController(Mock.Of<ISupportTicketService>(),
            Mock.Of<INotificationService>(), uow.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Delete("nonexistent");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Ticket not found.");
        controller.TempData["IsError"].Should().Be(true);
    }
}
