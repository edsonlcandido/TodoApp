using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("todos") ?? "Data Source=todos.db";
builder.Services.AddSqlite<TodoDb>(connectionString);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c=>{
    c.SwaggerDoc("v1",
    new OpenApiInfo{
        Title = "Todo API",
        Description = "Keep track of your tasks",
        Version = "v1"
    });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c=>{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1");
});

app.MapGet("/", () => "Hello World!");

app.MapGet("/todos", async (TodoDb db) => await db.Todos.ToListAsync());

app.MapPost("/todos", async (TodoDb db, TodoItem item) => {
    await db.Todos.AddAsync(item);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{item.Id}", item);
});

app.MapGet("/todos/{id}", async (TodoDb db, int id) => await db.Todos.FindAsync(id));

app.MapPut("/todos/{id}", async (TodoDb db, int id, TodoItem item) => {
    var todo = await db.Todos.FindAsync(id);
    if(todo is null) return Results.NotFound();
    todo.Item = item.Item;
    todo.Complete = item.Complete;
    db.Todos.Update(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todos/{id}", async (TodoDb db, int id) => {
    var todo = await db.Todos.FindAsync(id);
    if(todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

class TodoItem{
    public int Id{get; set;}
    public string? Item {get; set;}
    public bool Complete {get; set;}
}

class TodoDb : DbContext{
    public TodoDb(DbContextOptions<TodoDb> options) : base(options){
        //verify if database exists, if not create it
        if(!Database.CanConnect()){
            Database.EnsureCreated();
            Database.Migrate();
        }
    }
    public DbSet<TodoItem> Todos {get; set;}

    //if database does not exist, create it
    protected override void OnModelCreating(ModelBuilder modelBuilder){
        
        modelBuilder.Entity<TodoItem>().ToTable("Todo");
        //seed data
        modelBuilder.Entity<TodoItem>().HasData(
            new TodoItem{Id = 1, Item = "Do this", Complete = false},
            new TodoItem{Id = 2, Item = "Do that", Complete = false},
            new TodoItem{Id = 3, Item = "Do something else", Complete = false}
        );
    }

}