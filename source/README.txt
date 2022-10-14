To run, after opening DADProjSolution, simply run the Puppet Master process.

Currently it will initiate 3 Boney processes and 3 Bank processes by reading the global config.txt file.

Due to the lack of a complete Global Adversary/Scheduler and Bank Leader Election solution,
Bank servers will loop 3 times to simulate 3 timeslots, and in each iteration they will:
	-Randomly generate a value in the range(4,7) to simulate leader election.
	-Create a thread to call the GRPC service for each Boney
	-Execute the CompareAndSwap service with the candidate value and print out the result
That said each Bank process will print out the consensus result for each slot at most three times in this build.

