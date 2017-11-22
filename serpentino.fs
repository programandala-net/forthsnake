#! /usr/bin/env gforth

\ Serpentino
: version s" 0.7.0+201711221632" ;

\ Description: A simple snake game written in Forth for Gforth
\ and Solo Forth. Under development.

\ Author: Marcos Cruz (programandala.net)
\ http://programandala.net
\ http://github.com/programandala-net/serpentino

\ Last modified 201711221611
\ See change log at the end of the file

\ =============================================================
\ License

\ You may do whatever you want with this work, so long as you
\ retain all copyright, credit and authorship notices, and this
\ license.  There is no warranty.

\ =============================================================
\ Credit

\ Original code by Robert Pfeiffer:
\ <https://github.com/robertpfeiffer/forthsnake>.

\ ==============================================================

: random-range ( n1 n2 -- n3 ) over - utime + swap mod + ;

variable delay
  \ Frame delay in ms.

200 constant initial-delay
  \ Initial value of `delay`, in ms.

200 constant max-length
  \ Maximum length of the snake.

cols 2 - constant arena-width

rows 4 - constant arena-height

2 cells constant /segment
  \ Size of each snake's segment.

create snake  max-length /segment * allot
  \ Snake's segments. Each segment contains its coordinates.

2variable apple
  \ Coordinates of the apple.

variable head>
  \ Number of the head segment.

variable length
  \ Snake's current length.

2variable direction
  \ Snake's current direction, as coordinates' increments.

: segment ( n -- a )
  head> @ + max-length mod /segment * snake + ;
  \ Convert segment number _n_ to its address _a_.

: head ( -- a ) 0 segment ;
  \ _a_ is the address of the head segment.

: clash? ( a1 a2 -- f ) 2@ rot 2@ d= ;
  \ Are the coordinates contained in _a1_ equal to the
  \ coordinates contained in _a2_?

: cross? ( a -- f ) head clash? ;
  \ Does the head cross segment _a_?

: random-coordinates ( -- x y ) 1 arena-width  random-range
                                1 arena-height random-range ;

: new-apple ( -- ) random-coordinates apple 2! ;

: grow ( -- ) 1 length +! ;

: eat-apple ( -- ) grow new-apple ;

: coords+ ( n1 n2 x1 y1 -- x2 y2 ) rot + -rot + swap ;
  \ Update coordinates _x1 y1_ with increments _n1 n2_,
  \ resulting coordinates _x2 y2_.

: move-head ( -- ) head> @ 1- max-length mod head> ! ;

: step ( n1 n2 -- ) head 2@ move-head coords+ head 2! ;

-1  0 2constant left

 1  0 2constant right

 0  1 2constant down

 0 -1 2constant up

: wall? ( -- f ) head 2@ 1 arena-height within swap
                          1 arena-width  within and 0= ;

: crossing? ( -- f )
  length @ 1 ?do  i segment cross? if unloop true exit then
             loop false ;

: apple? ( -- f ) head apple clash? ;

: dead? ( -- f ) wall? crossing? or ;

: .frame ( -- )
  0 0 at-xy
  arena-width  0 ?do ." +" loop
  arena-height 0 ?do arena-width i at-xy ." +" cr ." +" loop
  arena-width  0 ?do ." +" loop cr ;

: .snake ( -- )
  0 segment 2@ at-xy ." O"
  length @ 1 ?do i segment 2@ at-xy ." o" loop ;

: .apple ( -- ) apple 2@ at-xy ." Q" ;

: render ( -- )
  page .snake .apple .frame cr length @ . ;

: new-snake ( -- )
  head> off 3 length !
  arena-width 2/ arena-height 2/ snake 2!
  up direction 2!
  left step left step left step left step ;

: init ( -- ) initial-delay delay ! new-snake new-apple ;

k-down  value down-key
k-left  value left-key
k-right value right-key
k-up    value up-key

: key>direction ( u -- n1 n2 )
  case
    down-key  of down  endof
    left-key  of left  endof
    right-key of right endof
    up-key    of up    endof
    direction 2@ rot
   endcase ;

: (rudder) ( -- n1 n2 )
  ekey ekey>fkey if   key>direction 2dup direction 2!
                 else direction 2@ then ;

: rudder ( -- n1 n2 ) ekey? if   (rudder)
                            else direction 2@ then ;

: lazy ( -- ) delay @ ms ;

: (game) ( -- ) render lazy rudder step
                apple? if eat-apple then ;

: center-y ( -- row ) rows 2/ ;

: center-x ( len -- col ) cols swap - 2/ ;

: center-xy ( len -- col row ) center-x center-y ;

: at-center-xy ( len -- ) center-xy at-xy ;

: type-center ( ca len -- ) dup at-center-xy type ;

: +type-center ( ca len n -- )
  >r dup center-xy r> + at-xy type ;

: unhome ( -- ) cols 1- rows 1- at-xy ;
  \ Set the cursor at the bottom right position of the screen.

: game-over ( -- ) s" **** GAME OVER **** " type-center unhome
                   2000 ms key drop ;

: game ( -- ) begin (game) dead? until game-over ;

: version$ ( -- ca len ) s" Version " version s+ ;

: splash-screen ( -- )
  page s" ooO SERPENTINO Ooo" -2 +type-center
                         version$ type-center
         s" programandala.net" 2 +type-center unhome
  2000 ms ;

: run ( -- ) begin splash-screen init game again ;

run

\ =============================================================
\ Change log

\ 2017-11-22: Fork from Robert Pfeiffer's forthsnake
\ (https://github.com/robertpfeiffer/forthsnake). Change source
\ style.  Rename words. Factor. Use constants and variables.
\ Use full screen. Draw the head apart. Document. Simplify
\ handling of directions.
