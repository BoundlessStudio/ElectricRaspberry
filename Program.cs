using ElectricRaspberry.Controllers;
using ElectricRaspberry.Extensions;
using ElectricRaspberry.Models;
using ElectricRaspberry.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptionsWithValidateOnStart<AiSettingsModel>().Bind(builder.Configuration.GetSection("Ai")).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptionsWithValidateOnStart<TableSettingsModel>().Bind(builder.Configuration.GetSection("Table")).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptionsWithValidateOnStart<GraphSettingsModel>().Bind(builder.Configuration.GetSection("Graph")).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDefaultSwaggerGen(builder.Environment.IsDevelopment());
builder.Services.AddDefaultCors();
builder.Services.AddDefaultAuthentication(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddOpenAIClient();
builder.Services.AddSingleton<CosmosGraphService>();
builder.Services.AddSingleton<CosmosTableService>();
builder.Services.AddSingleton<GraphDatabaseController>();
builder.Services.AddSingleton<TableDatabaseController>();
builder.Services.AddSingleton<FileController>();
builder.Services.AddSingleton<VectorStoreController>();
builder.Services.AddSingleton<FileAssociationsController>();
builder.Services.AddSingleton<AssistantController>();

var app = builder.Build();

app.MapSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDeveloperExceptionPage();
app.MapHub<ClientHub>("/hub/feed");
app.Map("/", () => Results.Redirect("/swagger/"));

var api = app.MapGroup("/api"); //.DisableAntiforgery()

if(!app.Environment.IsDevelopment())
  api.AddAuthorization();

var assistantController = app.Services.GetRequiredService<AssistantController>();
var fileController = app.Services.GetRequiredService<FileController>();
var vectorsController = app.Services.GetRequiredService<VectorStoreController>();
var fileAssociationsController = app.Services.GetRequiredService<FileAssociationsController>();
var graphController = app.Services.GetRequiredService<GraphDatabaseController>();
var tableController = app.Services.GetRequiredService<TableDatabaseController>();

api.MapPost("/assistants/", assistantController.Create).WithName("Create Assistant");
api.MapGet("/assistants/{id}", assistantController.Get).WithName("Get Assistant");
api.MapPut("/assistants/{id}", assistantController.Modify).WithName("Modify Assistant");
api.MapDelete("/assistants/{id}", assistantController.Delete).WithName("Delete Assistant");

api.MapPost("/files", fileController.Upload).DisableAntiforgery().WithName("Upload File");
api.MapGet("/files/{id}", fileController.Download).WithName("Download File");
api.MapGet("/files/{id}/info", fileController.Get).WithName("Get File Info");
api.MapDelete("/files/{id}", fileController.Delete).WithName("Delete File");

api.MapPost("/store/", vectorsController.Create).WithName("Create Vector Store");
api.MapGet("/store/{id}", vectorsController.Get).WithName("Get Vector Store");
api.MapPut("/store/{id}", vectorsController.Modify).WithName("Modify Vector Store");
api.MapDelete("/store/{id}", vectorsController.Delete).WithName("Delete Vector Store");

api.MapPost("/store/{id}/files", fileAssociationsController.Create).WithName("Create File Association");
api.MapGet("/store/{id}/files", fileAssociationsController.List).WithName("List File Associations");
api.MapGet("/store/{storeId}/files/{fileId}", fileAssociationsController.Get).WithName("Get File Association");
api.MapDelete("/store/{id}/files", fileAssociationsController.Delete).WithName("Remove File Association");

api.MapPost("/dbs/{id}/graphs", graphController.Create).WithName("Create Graph");
api.MapGet("/dbs/{id}/graphs", graphController.List).WithName("List Graphs");
api.MapPost("/dbs/{databaseId}/graphs/{collectionId}", graphController.Statements).WithName("Upsert Graph");
api.MapGet("/dbs/{databaseId}/graphs/{collectionId}", graphController.Query).WithName("Query Graph");
api.MapDelete("/dbs/{databaseId}/graphs/{collectionId}", graphController.Delete).WithName("Delete Graph");

api.MapPost("/dbs/{id}/tables", tableController.Create).WithName("Create Table");
api.MapGet("/dbs/{id}/tables", tableController.List).WithName("List Tables");
api.MapDelete("/dbs/{databaseId}/tables/{collectionId}", tableController.Delete).WithName("Delete Table");
api.MapPost("/dbs/{databaseId}/tables/{collectionId}/records", tableController.Upsert).WithName("Upsert Record");
api.MapGet("/dbs/{databaseId}/tables/{collectionId}/records", tableController.Query).WithName("Query Records");
api.MapGet("/dbs/{databaseId}/tables/{collectionId}/records/{id}", tableController.Get).WithName("Get Record");
api.MapDelete("/dbs/{databaseId}/tables/{collectionId}/records/{id}", tableController.Remove).WithName("Delete Record");

app.Run();