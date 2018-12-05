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
#include <time.h>

#define PORT 7777
#define MAXQUEUE 10

#pragma region Database logic
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
    struct dirent *de; // Pointer for directory entry 
    DIR *dr = opendir("./Database"); // opendir() returns a pointer of DIR type.

    if (dr == NULL)  // opendir returns NULL if couldn't open directory 
    { 
        printf("Error: Could not access database. Aborting...\n"); 
        return "Error"; 
    } 
    printf("Accessed database\n");

    //allocate memory for result
    char *subdirectories = malloc(2048);
    if(!subdirectories){
        printf("Error: Cannot allocate memory for the result. Aborting...\n");
        return "Error";
    }

    //read directory content
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

int ReturnListOfAnimals(int clientDescriptior, int clientID)
{
    long fileLength;
    char* listOfItems = ReturnListOfItems();
    int listSize = sizeof(listOfItems);
    char gotowosc[2];
    
    if(send(clientDescriptior, &listSize, sizeof(int),0) != sizeof(int)	)
    {
        printf("Pierwszy send nie powiodl sie :<\n");
        return -1;
    }
    printf("Pierwszy send powiodl sie");

    if(recv(clientDescriptior, &gotowosc, 2,0)<2)
    {
        printf("recv nie powiodl sie\n");
        return -1;
    }
    if(send(clientDescriptior, &listOfItems, listSize,0) != sizeof(listOfItems))
    {
        printf("Drugi send blad chyba :<\n");
        return -1;
    }
    return 0;
}


#pragma endregion
void WaitForCommand(int clientDescriptior, int clientID)
{
    char command[2];
    while(1)
    {
        printf("Czekam na decyzje od %d co robic\n", clientID);
        if(recv(clientDescriptior, command, 2,0) <=0)
        {
            printf("Error :(\n");
        }
        printf("Klient uzyl komendy: %s\n", command);

        if(command[0] == 'l')
        {
            if(ReturnListOfAnimals(clientDescriptior,clientID)<0)
            {
                printf("Error :(\n");
                return;
            }
        }
        if(command[0] == 's')
        {

        }
        if(command[0] == 'r')
        {
            
        }
        if(command[0] == 'q')
        {
            printf("Wszystko oki, klient chce wyjsc(a przynajmniej miejmy taka nadzieje)\n");
            return;
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
