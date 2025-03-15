# Design and Implementation of Distributed Applications - Boney Bank
The goal of the Boney Bank project is to develop a three tier system that emulates the
behaviour of simple but reliably fault-tolerant bank application. Boney Bank has a first
tier composed of clients who deposit and withdraw money from a banking account. The
operations are submitted to a set of bank server processes, the second tier, that use
primary-backup replication to ensure the durability of the account information. Finally,
in the third tier, is the Boney system, a distributed coordination system that the bank
server processes use to determine which one of the bank servers in the second tier is the
primary server.

Implemented in C# for the Design and Implementation of Distributed Applications 2022/2023 course at Instituto Superior Técnico.
