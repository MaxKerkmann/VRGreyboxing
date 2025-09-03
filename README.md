# User Manuel

Hier eine kurze Anleitung zur Installation und Konfiguration des Tools 

### Installation

Das System wurde in der Unity Version 6000.0.25f1 erstellt und auch nur in diesem getestet. Es kann in einem  leeren Projekt oder in einem bereits bestehenden Projekt installiert werden.

Innerhalb des Projekts kann das Package entweder über den Package Manager unter Window/Package Manager installiert werden oder direkt über die beigelegte .unitypackage Datei.

Innerhalb des Package Managers kann über das Plus Symbol ein Package über eine git URL hinzugefügt werden: https://github.com/MaxKerkmann/VRGreyboxing.git
Zur Verwendung der Package Datei muss diese in die Projekt Dateien innerhalb des Unity-Fensters gezogen werden. Dadurch öffnet sich ein Import Fenster, dass die Dateien innerhalb des Package-Ordners installiert.

In beiden Fällen müssen außerdem die Starter Assets des XR Interaction Toolkits importiert werden. Dies erfolgt über den Package Manager mit der Auswahl des installiertem Package XR Interaction Toolkit im Reiter Samples.
Dadurch sollten auch alle angezeigten Error Logs auf Konsole verschwinden.

Die weitere Installation hängt von dem verwendeten Head-Mounted Display ab. Zur Entwicklung wurde die HTC Vive Focus Vision verwendet und die weitere Installationsbeschreibung ist auf dieses Gerät abgestimmt. Hierbei wird die Verbindung der VR-Brille zu Unity konfiguriert.
Unter VIVE/OpenXR Installer/Install or Update latest version wird das VIVE OpenXR Plugin Version 2.5.1 installiert.

### Konfiguration

Innerhalb der Projects Settings unter Edit/Project Settings... müssen unterhalb der Kategorie XR Plug-in Management verschiedene Einstellungen vorgenommen werden:

Unter Project Validation sollten zunächst alle angezeigten Probleme behoben werden. Dazu sollte die Verwendung des angezeigten Fix All Button genügen bis auf einen Punkt. Für diesen muss unter dem Bereich OpenXR ein Interaction Profile hinzugefügt werden. Hier ist das Profil *VIVE Cosmos Controller Interaction* auszuwählen.
Damit sind die Project Settings abgeschlossen.

Zur Verwendung und Konfiguration des VRGreyboxing Tools muss dazu das entsprechende Editor Fenster über  Tools/Greyboxing Editor geöffnet werden.
Über dieses Fenster kann der Greyboxing Modus über den *Start Greyboxing*-Button gestartet werden. Es kann konfiguriert werden, ob alles Szenen im Projekt zur Auswahl innerhalb des Tools stehen sollen oder nur die, die innerhalb des Build Profiles definiert sind. Außerdem kann der Speicherort für Prefabs die innerhalb des Tools gespeichert festgelegt werden, sowie die Ordner aus denen Prefabs zur Platzierung innerhalb des Tools bezogen werden sollen. Ist die Liste leer werden alle Prefabs des Projekts verwendet.

Zuletzt besteht noch die Möglichkeit sich die Konfigurationsdatei anzuzeigen, in welcher die Daten des Tools gespeichert werden.
