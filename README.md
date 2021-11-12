# network_com_hw: Take-home programming project
## Objective
Build two independent inter-communicating processes (A & B) able to exchange data using two different means of data transfer/messaging services. The processes could be headless - run from a command line, or have a GUI (You will provide instructions for using them).

For communication use any two of several methods available, such as…
- Network sockets
- ZeroMQ (or equivalent for chosen programming language)
- Netty
- Other messaging brokers (could use a test broker hosted online)
- Other

Using one method of communication, Process A must connect with B and send contents of the provided data file cad_mesh.stl (a CAD geometry). On receiving the data, B, which has earlier established a second channel of communication with A, must return the data. A will then save the received data in a file called output.stl (cad_mesh.stl must match output.stl).

## Requirements
- The application/s may run on a single Windows or Linux workstation; may be connected to the web or not. Alternatively the two processes A & B could be deployed and run on two independent work stations. 
- Developed in the candidate's choice of programming language.
- Candidates must provide executables for functional evaluation, as well as code for review and discussion during subsequent follow-up interview discussion. The delivered package must include any necessary configuration or other files.
- The deliverables must include documentation that could be followed to download, install and/or run the application.
- The code must be organized and well-documented. 
- The effort is expected to take between 2 - 8 hours based on the candidate’s experience in network application development.

## Grading Criteria
- We’re looking for code that is clean, readable, performant and maintainable.
- The developer must think through the process of deploying and using the solution and provide necessary documentation.
- The choice of messaging services used will not matter as long as the final code performs as expected. 

## Optional Simpler Scope (to reduce candidate's time and effort)
- Implement one-way communication from process A to B using any single method of data transfer.
- Process B is to save received data in a file called output.stl

## Optional Challenge (beyond original scope)
- In Process B, implement a parser that reads the .stl file, extracts each vertex in the file and generates an Output.csv file containing the vertices.
- The Output.csv file must be formatted to contain data (x,y,z) for each vertex on an independent line. The positional data coordinate values, each a float, must have 4 significant digits following the decimal point. 
- Return the output.csv file to process B.
- If not familiar, the candidate is expected to independently conduct research online to understand the format of an .STL file, which is a commonly used CAD format.

## Submission
In order to submit the assignment, do the following:

1. Navigate to GitHub's project import page: [https://github.com/new/import](https://github.com/new/import)

2. In the box titled "Your old repository's clone URL", paste the homework repository's link: [https://github.com/Machina-Labs/network_com_hw](https://github.com/Machina-Labs/network_com_hw)

3. In the box titled "Repository Name", add a name for your local homework (ex. `network_com_soln`)

4. Set privacy level to "Public", then click "Begin Import" button at bottom of the page.

5. Develop your homework solution in the cloned repository and push it to Github when you're done. Extra points for good Git hygiene.

6. Send us the link to your repository.
