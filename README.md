# Fitness-App
Customisable fitness application for me 

## Build, test, run

```
dotnet build
dotnet test
dotnet run --project src/Web --launch-profile lan
```

The `lan` profile listens on all interfaces at port 5276. Open `http://<your-PC-LAN-IP>:5276` on a phone on the same network (allow port 5276 through the Windows firewall if the phone can't connect). Use `--launch-profile http` to open a browser on localhost instead.
