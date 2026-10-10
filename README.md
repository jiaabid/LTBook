# ToolBook

A web app for booking shared lab tools. Researchers can book a tool for a time slot, see when a busy tool will likely be free, and ask questions about how to use a tool. Answers come from the tool's manual.

## Prerequisites

Install these first:

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (runs the SQL Server database)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22](https://nodejs.org/)
- [Ollama](https://ollama.com/download) (free local AI for the "Ask about this tool" feature)

Then download the two AI models once (about 2.3 GB):

```bash
ollama pull llama3.2:3b
ollama pull nomic-embed-text
```

## Start the project

Run each command from the project folder. Use a separate terminal for steps 2 and 3, because they keep running.

**1. Start the database**

```bash
docker compose up -d
```

**2. Start the API** (http://localhost:5042)

```bash
dotnet run --project src/ToolBook.Host
```

The database tables, sample tools and sample manuals are created automatically on first start.

**3. Start the web app** (http://localhost:5173)

```bash
cd web
npm install
npm run dev
```

**4. Open http://localhost:5173 and sign in**

- Email: `admin@toolbook.local`
- Password: `Admin_Dev_Pass1!`

You can also register a new user from the sign-in page.

To try the assistant, open **Scanning Electron Microscope** and ask: *"How do I vent the chamber?"*
