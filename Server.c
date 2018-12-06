#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <dirent.h>
#include <sys/types.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <sys/stat.h>
#include <unistd.h>
#include <stdint.h>

#define PORT 7777

#define MAXQUEUE 10

#pragma region Database logic
char ok[2] = "OK";

//FILE* file;
//struct stat fileinfo;

void SetDatabase (){
    printf("\n*** Requested setting a database ***\n");
    char directory [200] = "./Database";
    int code = mkdir(directory, S_IRWXU | S_IRWXG | S_IRWXO);
    if(code)
    {
        printf("Database is already set.\n");
    }
    else
    {
        printf("No database directory found! Creating database.\n");
    }
    printf("Action finished\n");
    return;
}
int CreateFolder (char name []){
    printf("\n*** Requested creating a new folder ***\nFolder name: %s\n", name);
    char directory [200] = "./Database/%s";
    strcat(directory, name);
    printf("Creating a directory under %s\n", directory);
    int code = mkdir(directory, S_IRWXU | S_IRWXG | S_IRWXO);
    if(code)
    {
        printf("Error: Couldn't create a directory. Error code: %d. Aborting...\n", code);
        return -1;
    }
    else
    {
        printf("Directory successfully created.\n");
        printf("Action finished\n");
        return 0;
    }
}
char *ReturnListOfItems(){
    printf("\n*** Action: Getting list of available items in database ***\n");
    struct dirent *de;
	DIR *dr = opendir("./Database");

    if (dr == NULL) 
    { 
        printf("Error: Could not access database. Aborting...\n"); 
        return "Error"; 
    } 
    printf("Accessed database\n");

    char *subdirectories = malloc(2048);
    if(!subdirectories){
        printf("Error: Cannot allocate memory for the result. Aborting...\n");
        return "Error";
    }

    printf("Reading results\n");
    while ((de = readdir(dr)) != NULL){
        strcat(subdirectories,de->d_name); 
        strcat(subdirectories,"\n");
    }
  
    closedir(dr);

    printf("Results:\n%s",subdirectories);
    printf("Action finished\n");
    return subdirectories;
}
int SearchForItem (char name []){
    printf("\n*** Action: Searching database for item \"%s\" ***\n", name);
    char *items = ReturnListOfItems();
    if(items=="Error"){
        printf("Error: Cannot search database. Aborting...\n");
        return -1;
    }
    if(strstr(items, name) != NULL) {
        printf("Item \"%s\" found\n", name);
        printf("Action finished\n");
        return 0;
        }
    else{
        printf("Item \"%s\" not found\n", name);
        printf("Action finished\n");
        return -1;
    }
}
int SendListOfAnimals(int clientDescriptior, int clientID){
    printf("\n*** Action: Send list of animals to client %d ***\n", clientID);
    long fileLength;
    printf("Getting list of items.\n");
    char* listOfItems = ReturnListOfItems();
	printf("%s\n",listOfItems);
    if(send(clientDescriptior, listOfItems, 2048,0) != 2048)
    {
        printf("Error: Couldn't send the list. Aborting...\n");
        return -1;
    }
    printf("List of animals successfully sent\n");
    printf("Action finished\n");
    return 0;
}
void SendFiles(int clientDescriptior, int clientID)
{
    printf("\n*** Action: Send files to client %d ***\n", clientID);
    printf("Creating buffers\n");
    char buffer[2];
	char name[100];
	char *path = "./Database/";
	memset(name,0,100);

    /*OK na d*/
	if (send(clientDescriptior, ok, sizeof(ok),0) != sizeof(ok))
	{
		printf("Error: Couldn't send confirmation to client %d. Aborting...\n", clientID);
		return;
	}
	printf("Wyslano \"OK\" \n");

    /*recv na zwierze*/
	if (recv(clientDescriptior, &name, 100,0) <= 0)
	{
		printf("recv nie powiodl sie\n");
		return;
	}
	printf("Klient chce pobrac: %s\n", name);

    /*Liczę pliki w podfolderze*/
    char directory[100];
    sprintf (directory, "%s%s", path, name);
	printf("%s\n", directory);

    struct dirent *de;
	DIR *dr = opendir(directory);

    if (dr == NULL) 
    { 
        printf("Error: Could not access folder. Aborting...\n"); 
        return; 
    } 
    printf("Accessed folder\n");

    int subdirectoriesCount =-2;

    while ((de = readdir(dr)) != NULL){
        subdirectoriesCount++;
    }
    closedir(dr);
    printf("%d files in %s\n",subdirectoriesCount,directory);

    /*Wyslij liczbe plikow*/

    char subdirCount[2];
    sprintf(subdirCount, "%d", subdirectoriesCount);
    
    /*printf("SIZE OF INT %ld",sizeof(int32_t));*/
    if (send(clientDescriptior, subdirCount, 2,0) != 2)
	{
		printf("Couldn't send file count\n");
		return;
	}
    else{ printf("File count sent\n"); }

    /*recv ok po file count*/
	if (recv(clientDescriptior, &buffer, 2,0) <= 0)
	{
		printf("recv nie powiodl sie\n");
		return;
	}
	printf("Klient: %s\n", buffer);

    /*Pobierz liste plikow do wysłania*/
    /*struct dirent *de;*/
    char pathToFiles[100];
    sprintf(pathToFiles,"./Database/%s",name);
	DIR *dir = opendir(pathToFiles);
    if (dir == NULL) 
    { 
        printf("Error: Could not access files. Aborting...\n"); 
        return; 
    } 
    printf("Accessed files\n");

    char subdirectories[10][150];
    char subdirectoryfile[150];
    if(!subdirectories){
        printf("Error: Cannot allocate memory for the result. Aborting...\n");
        return;
    }

    printf("Reading results\n");
    int fileCounter=0;
    while ((de = readdir(dir)) != NULL){
        strcat(subdirectories[fileCounter],pathToFiles);
        strcat(subdirectories[fileCounter],"/");
        strcat(subdirectories[fileCounter],de->d_name); 
        strcat(subdirectories[fileCounter],"\n");
        fileCounter++;
    }
  
    closedir(dir);

    printf("Results:\n%s",subdirectories[1]);
    printf("Action finished\n");

    /*-----------------------------*/
    int i;
    for(i=0;i<subdirectoriesCount;i++ )
    {
        struct stat fileinfo;
        FILE* plik;
        long dl_pliku;
        char *sciezka = subdirectories[i+2];
        printf("%s", sciezka);
        /*pobieram wielkosc pierwszego pliku*/
        if (stat("./Database/Dupies/text.txt", &fileinfo) < 0)
        {
            printf("Cannot get file size\n");
            return;
        }
        printf("File size: %ld\n", fileinfo.st_size);
        
        char fileSize[20];
        sprintf(fileSize, "%ld", fileinfo.st_size);
        if (send(clientDescriptior, fileSize, strlen(fileSize), 0) != strlen(fileSize))
        {
            printf("Couldn't send file size\n");
            return;
        }
        printf("File size sent");
        
        /*recv ok po file size*/
        if (recv(clientDescriptior, &buffer, 2,0) <= 0)
        {
            printf("recv nie powiodl sie\n");
            return;
        }
        printf("Klient: %s\n", buffer);

        /*Przesyłanie pliku*/
        dl_pliku = fileinfo.st_size;
        long bytesSent = 0;
        unsigned char bufor[1024];

        plik = fopen(sciezka, "rb");
        if (plik == NULL)
        {
            printf("Couldn't acces the file\n");
            return;
        }
        
        while (bytesSent < dl_pliku)
        {
            long przeczytano = fread(&bufor, 1, 1024, plik);
            long wyslano = send(clientDescriptior, &bufor, przeczytano, 0);
            if (przeczytano != wyslano)
                break;
            bytesSent += wyslano;
            printf("Potomny: wyslano %ld bajtow\n", bytesSent);
            printf("Bufor: %s", bufor);
        }
        
        if (bytesSent == dl_pliku)
            printf("Potomny: plik wyslany poprawnie\n");
        else
            printf("Potomny: blad przy wysylaniu pliku\n");
        fclose(plik);

        /*recv ok po file*/
        if (recv(clientDescriptior, &buffer, 2,0) <= 0)
        {
            printf("recv nie powiodl sie\n");
            return;
        }
        printf("Klient: %s\n", buffer);
    }
    

	return;
}

#pragma endregion
void WaitForCommand(int clientDescriptior, int clientID)
{
    char command[1];
    while(1)
    {
        printf("Czekam na decyzje od %d co robic\n", clientID);
        if(recv(clientDescriptior, command, 1,0) <=0)
        {
            printf("Polaczenie zerwane\n");
			break;
        }
        printf("Klient uzyl komendy: %s\n", command);

        if(command[0] == 'l')
        {
            if(SendListOfAnimals(clientDescriptior,clientID)<0)
            {
                printf("Error :(\n");
                return;
            }
        }
        if(command[0] == 's')
        {

        }
        if(command[0] == 'd')
        {
			SendFiles(clientDescriptior, clientID);
        }
    }
}

int RunServerConnection(){
    int clientDescriptor;
    int serverSocketDescriptor = socket(AF_INET, SOCK_STREAM, 0);
    struct sockaddr_in address;
     
    printf("\n*** STARTING SERVER ON PORT %d ***\n", PORT);    

        address.sin_family = AF_INET;
        address.sin_port = htons(PORT);
        address.sin_addr.s_addr = INADDR_ANY;
        memset(address.sin_zero, 0, sizeof(address.sin_zero));

    socklen_t addressLength = sizeof(struct sockaddr_in);
    SetDatabase();
    printf("\n*** BINDING PORT %d ***\n", PORT);

    if (bind(serverSocketDescriptor, (struct sockaddr*) &address, addressLength) < 0)
    {
        printf("ERROR: Bind failed. Aborting...\n");
        return 1;
    }

    printf("Bind successful\n");
    
    printf("\n*** LISTENING ON PORT %d. SETTING QUEUE TO %d ***\n", PORT, MAXQUEUE);
    listen(serverSocketDescriptor, MAXQUEUE);
    printf("\n*** STARTING INFINITE LOOP ***\n");
    printf("----------------------------------------\n");
    printf("( ͡° ͜ʖ ͡°) | Welcome. Call me Mr Jenkins. I will take care of your clients from now on.\n");
    printf("----------------------------------------\n\n");

    while(1)
    {
        addressLength = sizeof(struct sockaddr_in);

        printf("( ͡° ͜ʖ ͡°) Mr Jenkins | Waiting for new client\n");
        clientDescriptor = accept(serverSocketDescriptor, (struct sockaddr*) &address, &addressLength);
        if (clientDescriptor < 0)
        {
            printf("( ͡° ʖ̯ ͡°) Mr Jenkins | But he didn't accept us...\n");
            continue;
        }

        printf("( ͡° ͜ʖ ͡°) Mr Jenkins | Incoming connect from %s:%u\n", 
            inet_ntoa(address.sin_addr),
            ntohs(address.sin_port)
            );

        printf("( ͡° ͜ʖ ͡°) Mr Jenkins | Assigning Mr Harold to take care of new client\n");
        if (fork() == 0) // child process
        {
            printf("(͠≖ ͜ʖ͠≖) Mr Harold | Serving client %s:%u\n", 
                inet_ntoa(address.sin_addr),
                ntohs(address.sin_port)
                );

            WaitForCommand(clientDescriptor, ntohs(address.sin_port));

            printf("(͠≖ ͜ʖ͠≖) Mr Harold | Closing port...\n");
            close(clientDescriptor);

            printf("(͠≖ ͜ʖ͠≖) Mr Harold | Killing this process... I'll be back when you need me\n");
            exit(0);
        }
        else
        {
            printf("( ͡° ͜ʖ ͡°) Mr Jenkins | Back to work. Still listening...\n"); // main process
            continue;
        }
    }
    return 0;
}

#pragma endregion

#pragma region User Interaction logic
#pragma endregion
int main()
{
    printf("Starting server side C application\n");
    RunServerConnection();

    return 0;
}
