# QRcode_Transfer_CSharp
A simple Fin-tech project that uses QRcode to share information and transfer money. 

Threads, Threadpool and Locks have been used in this project for simulating the situation in real server ...

# Structure
This project use WPF to develop, XAML files(.xaml) are for front-end function and C# files(.cs) are for back-end function.

Account and Transfer_Info objects are information container.

Bank and Central objects are simulating the real bank servers.

MainWindow objects is the representation of Central Bank moniter.

UserForm objects are the representation of the user side.

Note: those .png placed in the repository would have more illustration.

# Platform
VS2019

# Future Work
So far, this project is mainly for demonstrating QRcode generation and scanning, and the usage of some thread tools and locks.

Nevertheless, a real bank server or transfer machanism must be more complicate ...

If time permits, i would keep introducing functions and tools for completing a more sophisticated system, which may include SQL containers, , Cryptography, Base64, and so on.
