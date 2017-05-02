using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using EventFlow.Queries;
using EventFlow.Scenarios.Mvc5.Domain;

namespace EventFlow.Scenarios.Mvc5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICommandBus _commandBus;
        private readonly IQueryProcessor _queryProcessor;
        public HomeController(ICommandBus commandBus, IQueryProcessor queryProcessor)
        {
            _commandBus = commandBus;
            _queryProcessor = queryProcessor;
        }

        public async Task<ViewResult> Index()
        {
            ViewBag.Title = "Home Page";
            const int magicNumber = 42;
            var exampleId = ExampleId.NewComb();
            await _commandBus.PublishAsync(new ExampleCommand(exampleId, magicNumber), CancellationToken.None);

            var readModel = await _queryProcessor.ProcessAsync(new ReadModelByIdQuery<ExampleReadModel>(exampleId), CancellationToken.None);
            return View(readModel);
        }
    }
}