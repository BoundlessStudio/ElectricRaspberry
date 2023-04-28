using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors.Client;
using Microsoft.SemanticKernel.Planning;

// Load configuration
IConfigurationRoot configuration = new ConfigurationBuilder()
  .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
  .AddJsonFile(path: "appsettings.Development.json", optional: true, reloadOnChange: true)
  .AddEnvironmentVariables()
  .AddUserSecrets<Program>()
  .Build();

// Initialize logger
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
  {
    builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole().AddDebug();
  });

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

var graphApiConfiguration = configuration.GetRequiredSection("MsGraph").Get<MsGraphConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Microsoft Graph API.");
var openAIConfiguration = configuration.GetRequiredSection("OpenAI").Get<OpenAIConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Open AI.");
var bingConfiguration = configuration.GetRequiredSection("Bing").Get<BingConfiguration>() ?? throw new InvalidOperationException("Missing configuration for Bing.");

var graphServiceClient = await MSALHelper.CreateGraphServiceClientAsync(graphApiConfiguration, logger);

var kernelConfig = new KernelConfig()
  .AddOpenAIChatCompletionService("GPT-4", "gpt-4-0314", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextCompletionService("GPT-3.5", "gpt-3.5-turbo", openAIConfiguration.ApiKey, openAIConfiguration.OrgId)
  .AddOpenAITextEmbeddingGenerationService("Embeddings", "text-embedding-ada-002", openAIConfiguration.ApiKey, openAIConfiguration.OrgId);

var memoryStore = new VolatileMemoryStore(); 
// var memoryStore = new Microsoft.SemanticKernel.Connectors.Memory.Qdrant.QdrantMemoryStore("http://localhost", 6333, 1536);

IKernel myKernel = Kernel.Builder
  .WithConfiguration(kernelConfig)
  .WithLogger(logger)
  .WithMemoryStorage(memoryStore)
  .Build();

//@Monkey TODO: Test These Skills: Memory, Text, and MicrosoftService (Task/Calendar)
//@rainbow-pineapple TODO: Interact on ImportChatGptPluginSkillFromUrlAsync function with unit tests using https://www.wolframcloud.com/.well-known/ai-plugin.json
myKernel.RegisterMemorySkills();
myKernel.RegisterSemanticSkills("semantics");
myKernel.RegisterSystemSkills();
myKernel.RegisterFilesSkills();
myKernel.RegisterOfficeSkills();
myKernel.RegisterWebSkills(bingConfiguration.ApiKey);
myKernel.RegisterMicrosoftServiceSkills(graphServiceClient);
//@RGBKnights TODO: Test ChatGptPluginSkill
// await myKernel.ImportChatGptPluginSkillFromUrlAsync("WolframAlpha", new Uri("https://www.wolframcloud.com/.well-known/ai-plugin.json"));
//@RGBKnights TODO: Test OpenApiSkill 
// await myKernel.ImportOpenApiSkillFromUrlAsync("QuickChart", new Uri("https://quickchart.io/openapi.yaml"));

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) => {
  e.Cancel = true;
  cts.Cancel();
};



//  const string memoryCollectionName = "aboutMe";
//  // ========= Store memories using the kernel =========

// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info1", text: "My name is Andrea");
// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info2", text: "I work as a tourist operator");
// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info3", text: "I've been living in Seattle since 2005");
// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info4", text: "I visited France and Italy five times since 2015");

// // ========= Store memories using semantic function =========

// // Add Memory as a skill for other functions
// var memorySkill = new TextMemorySkill();
// myKernel.ImportSkill(new TextMemorySkill());

// // Build a semantic function that saves info to memory
// const string SAVE_FUNCTION_DEFINITION = @"{{save $info}}";
// var memorySaver = myKernel.CreateSemanticFunction(SAVE_FUNCTION_DEFINITION);

// var context = myKernel.CreateNewContext();
// context[TextMemorySkill.CollectionParam] = memoryCollectionName;
// context[TextMemorySkill.KeyParam] = "info5";
// context["info"] = "My family is from New York";
// await memorySaver.InvokeAsync(context);

// // ========= Test memory remember =========
// Console.WriteLine("========= Example: Recalling a Memory =========");

// context[TextMemorySkill.KeyParam] = "info1";
// var answer = await memorySkill.RetrieveAsync(context);
// Console.WriteLine("Memory associated with 'info1': {0}", answer);
// /*
// Output:
// "Memory associated with 'info1': My name is Andrea
// */

// // ========= Test memory recall =========
// Console.WriteLine("========= Example: Recalling an Idea =========");

// context[TextMemorySkill.LimitParam] = "2";
// string ask = "where did I grow up?";
// answer = memorySkill.Recall(ask, context);
// Console.WriteLine("Ask: {0}", ask);
// Console.WriteLine("Answer:\n{0}", answer);

// ask = "where do I live?";
// answer = memorySkill.Recall(ask, context);
// Console.WriteLine("Ask: {0}", ask);
// Console.WriteLine("Answer:\n{0}", answer);

// /*
// Output:

//     Ask: where did I grow up?
//     Answer:
//         ["My family is from New York","I\u0027ve been living in Seattle since 2005"]

//     Ask: where do I live?
//     Answer:
//         ["I\u0027ve been living in Seattle since 2005","My family is from New York"]
// */

// // ========= Use memory in a semantic function =========
// Console.WriteLine("========= Example: Using Recall in a Semantic Function =========");

// Build a semantic function that uses memory to find facts
// const string RECALL_FUNCTION_DEFINITION = @"
// Consider only the facts below when answering questions.

// About me: {{recall 'where did I grow up?'}}
// About me: {{recall 'where do I live?'}}

// Question: {{$query}}

// Answer:
// ";

// var aboutMeOracle = myKernel.CreateSemanticFunction(RECALL_FUNCTION_DEFINITION, maxTokens: 100);

// context["query"] = "Do I live in the same town where I grew up?";
// context[TextMemorySkill.RelevanceParam] = "0.8";

// var result = await aboutMeOracle.InvokeAsync(context);

// Console.WriteLine(context["query"] + "\n");
// Console.WriteLine(result);

/*
Output:

    Do I live in the same town where I grew up?

    No, I do not live in the same town where I grew up since my family is from New York and I have been living in Seattle since 2005.
*/


// const string memoryCollectionName = "qdrant";
// Console.WriteLine("== Printing Collections in DB ==");
// var collections = memoryStore.GetCollectionsAsync();
// await foreach (var collection in collections)
// {
//     Console.WriteLine(collection);
// }

// Console.WriteLine("== Adding Memories ==");

// var key1 = await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "cat1", text: "british short hair");
// var key2 = await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "cat2", text: "orange tabby");
// var key3 = await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "cat3", text: "norwegian forest cat");

// Console.WriteLine("== Printing Collections in DB ==");
// collections = memoryStore.GetCollectionsAsync();
// await foreach (var collection in collections)
// {
//     Console.WriteLine(collection);
// }

// Console.WriteLine("== Retrieving Memories Through the Kernel ==");
// MemoryQueryResult? lookup = await myKernel.Memory.GetAsync(memoryCollectionName, "cat1");
// Console.WriteLine(lookup != null ? lookup.Metadata.Text : "ERROR: memory not found");

// Console.WriteLine("== Retrieving Memories Directly From the Store ==");
// var memory1 = await memoryStore.GetWithPointIdAsync(memoryCollectionName, key1);
// var memory2 = await memoryStore.GetWithPointIdAsync(memoryCollectionName, key2);
// var memory3 = await memoryStore.GetWithPointIdAsync(memoryCollectionName, key3);
// Console.WriteLine(memory1 != null ? memory1.Metadata.Text : "ERROR: memory not found");
// Console.WriteLine(memory2 != null ? memory2.Metadata.Text : "ERROR: memory not found");
// Console.WriteLine(memory3 != null ? memory3.Metadata.Text : "ERROR: memory not found");

// Console.WriteLine("== Similarity Searching Memories: My favorite color is orange ==");
// var searchResults = myKernel.Memory.SearchAsync(memoryCollectionName, "My favorite color is orange", limit: 3, minRelevanceScore: 0.8);

// await foreach (var item in searchResults)
// {
//     Console.WriteLine(item.Metadata.Text + " : " + item.Relevance);
// }

// Console.WriteLine("== Removing Collection {0} ==", memoryCollectionName);
// await memoryStore.DeleteCollectionAsync(memoryCollectionName);

// Console.WriteLine("== Printing Collections in DB ==");
// collections = memoryStore.GetCollectionsAsync();
// await foreach (var collection in collections)
// {
//     Console.WriteLine(collection);
// }

// var goal = "Create a slogan for the BBQ Pit in London that specializes in Mustard Sauce then email it to jamie_maxwell_webster@hotmail.com with the subject 'New Marketing Slogan'";
// var planner = new SequentialPlanner(myKernel);
// var plan = await planner.CreatePlanAsync(goal);
// await plan.WriteAsync("plans");
// await plan.InvokeAsync(cancel: cts.Token);
// await plan.WriteAsync("plans");
// Console.WriteLine("Plan Complete");

// const string memoryCollectionName = "generic";
// var githubFiles = new Dictionary<string, string>()
//   {
//       ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
//           = "README: Installation, getting started, and how to contribute",
//       ["https://github.com/microsoft/semantic-kernel/blob/main/samples/notebooks/dotnet/02-running-prompts-from-file.ipynb"]
//           = "Jupyter notebook describing how to pass prompts from a file to a semantic skill or function",
//       ["https://github.com/microsoft/semantic-kernel/blob/main/samples/notebooks/dotnet/00-getting-started.ipynb"]
//           = "Jupyter notebook describing how to get started with the Semantic Kernel",
//       ["https://github.com/microsoft/semantic-kernel/tree/main/samples/skills/ChatSkill/ChatGPT"]
//           = "Sample demonstrating how to create a chat skill interfacing with ChatGPT",
//       ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel/Memory/VolatileMemoryStore.cs"]
//           = "C# class that defines a volatile embedding store",
//       ["https://github.com/microsoft/semantic-kernel/blob/main/samples/dotnet/KernelHttpServer/README.md"]
//           = "README: How to set up a Semantic Kernel Service API using Azure Function Runtime v4",
//       ["https://github.com/microsoft/semantic-kernel/blob/main/samples/apps/chat-summary-webapp-react/README.md"]
//           = "README: README associated with a sample chat summary react-based webapp",
//   };

// var input = myKernel.CreateNewContext();
// foreach (var entry in githubFiles)
// {
//   await input.Memory.SaveReferenceAsync(
//       collection: memoryCollectionName,
//       description: entry.Value,
//       text: entry.Value,
//       externalId: entry.Key,
//       externalSourceName: "GitHub"
//   );
// }
// Console.WriteLine($"Files added: {githubFiles.Count}");

// string ask = "how do I get started?";
// Console.WriteLine("Query: " + ask);
// Console.WriteLine();
// var memories = input.Memory.SearchAsync(memoryCollectionName, ask, limit: 5, minRelevanceScore: 0.77);

// await foreach (MemoryQueryResult memory in memories)
// {
//   Console.WriteLine("Title: " + memory.Metadata.Description);
//   Console.WriteLine("-URL:     : " + memory.Metadata.Id);  
//   Console.WriteLine("-Relevance: " + memory.Relevance);
//   Console.WriteLine();
// }


// const string memoryCollectionName = "aboutMe";
// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info1", text: "My name is Andrea");
// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info2", text: "I work as a tourist operator");
// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info3", text: "I've been living in Seattle since 2005");
// await myKernel.Memory.SaveInformationAsync(memoryCollectionName, id: "info4", text: "I visited France and Italy five times since 2015");


// var goal = "retrieve memory 'info1' then set it into a variable 'RESULTS' then save it as 'info5' then delete 'info1' then retrieve 'info2'";
// var planner = new SequentialPlanner(myKernel);
// var plan = await planner.CreatePlanAsync(goal);

// foreach (var step in plan.Steps) {
//   switch (step.SkillName)
//   {
//     case nameof(TextMemorySkill):
//       step.NamedParameters[TextMemorySkill.CollectionParam] = memoryCollectionName; // Overwrite whatever the AI comes up with...
//       break;
//     default:
//       break;
//   }
// }

// var cxt = myKernel.CreateNewContext();
// await plan.InvokeAsync(context: cxt);
// await plan.WriteAsync("plans");
// Console.WriteLine("Plan Complete");
// foreach (var variable in plan.State)
// {
//   Console.WriteLine(variable.Key + ": " + variable.Value); 
// }

// const string RECALL_FUNCTION_DEFINITION = @"
// Consider only the facts below when answering questions.

// About me: {{recall 'where did I grow up?'}}
// About me: {{recall 'where do I live?'}}

// Question: {{$query}}

// Answer:
// ";

// var aboutMeOracle = myKernel.CreateSemanticFunction(RECALL_FUNCTION_DEFINITION, maxTokens: 100);

// var goal = 
// @"create book about 'a boy and dog':
// 1. write a novel outline that has 5 chapters.
// 2. write the each chapter.
// 4. store the final novel in the output directory.";
// var planner = new SequentialPlanner(myKernel);
// var plan = await planner.CreatePlanAsync(goal);
// await plan.WriteAsync("plans");
// Console.WriteLine("Plan Created");
// var cxt = myKernel.CreateNewContext();
// await plan.InvokeAsync(context: cxt, cancel: cts.Token);
// Console.WriteLine("Plan Complete");
// await plan.WriteAsync("plans");
// foreach (var variable in plan.State)
// {
//   Console.WriteLine(variable.Key + ": " + variable.Value);
// }

//var planner = new ActionPlanner(myKernel);
var planner = new SequentialPlanner(myKernel);

var goal = @"Create an C# azure durable function that calls itself continuously and only returns after receiving an manual event to break then write the results to a 'function.md' in the 'output' directory.";
var plan = await planner.CreatePlanAsync(goal);
await plan.WriteAsync("plans");
Console.WriteLine("Plan Created");
var cxt = myKernel.CreateNewContext();
await plan.InvokeAsync(context: cxt, cancel: cts.Token);
Console.WriteLine("Plan Complete");
Console.WriteLine("Variables:");
await plan.WriteAsync("plans");
foreach (var variable in plan.State)
{
  Console.WriteLine(variable.Key + ": " + variable.Value);
}
