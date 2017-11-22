#! /usr/bin/env gforth

\ Serpentino

: version s" 0.10.0+201711221754" ;
\ See change log at the end of the file.

\ Description:
\
\ A simple snake game written in Forth
\ (http://forth-standard.org) for Gforth
\ (http://gnu.org/software/gforth) and (eventually) for Solo
\ Forth (http://programandala.net/en.program.solo_forth.html).
\
\ Under development.

\ Author: Marcos Cruz (programandala.net)
\ http://programandala.net
\ http://github.com/programandala-net/serpentino

\ =============================================================
\ License

\ You may do whatever you want with this work, so long as you
\ retain all copyright, credit and authorship notices, and this
\ license.  There is no warranty.

\ =============================================================
\ Credit

\ Original code by Robert Pfeiffer, 2009:
\ <https://github.com/robertpfeiffer/forthsnake>.

\ ==============================================================

: random-range ( n1 n2 -- n3 ) over - utime + swap mod + ;

variable delay
  \ Crawl delay in ms.

200 constant initial-delay
  \ Initial crawl delay in ms.

200 constant max-length
  \ Maximum length of the snake.

cols 2 - constant arena-width
rows 3 - constant arena-height
  \ Size of the arena, not including the wall.

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
  \ Return address _a_ of the snake's head segment.

: neck ( -- a ) 1 segment ;
  \ Return address _a_ of the snake's "neck" segment, ie.
  \ the segment after the head.

: tail ( -- a ) length @ segment ;
  \ Return address _a_ of the snake's tail segment.

: clash? ( a1 a2 -- f ) 2@ rot 2@ d= ;
  \ Are the coordinates contained in _a1_ equal to the
  \ coordinates contained in _a2_?

: cross? ( a -- f ) head clash? ;
  \ Does the head cross segment _a_?

: random-coordinates ( -- col row )
  1 arena-width  random-range
  1 arena-height random-range ;

: new-apple ( -- ) random-coordinates apple 2! ;

: .score ( -- ) 0 rows 1- at-xy length ? ;
  \ Display the score (the current length of the snake).

: grow ( -- ) 1 length +! .score ;
  \ Grow the snake and update the score.

: .apple ( -- ) apple 2@ at-xy ." Q" ;
  \ Display the apple.

: coords+ ( n1 n2 x1 y1 -- x2 y2 ) rot + -rot + swap ;
  \ Update coordinates _x1 y1_ with increments _n1 n2_,
  \ resulting coordinates _x2 y2_.

: move-head ( -- ) head> @ 1- max-length mod head> ! ;

: crawl ( n1 n2 -- ) head 2@ move-head coords+ head 2! ;
  \ Update the snake's position with coordinate increments _n1
  \ n2_.

-1  0 2constant left
  \ Left direction coordinate increments.

 1  0 2constant right
  \ Right direction coordinate increments.

 0  1 2constant down
  \ Down direction coordinate increments.

 0 -1 2constant up
  \ Up direction coordinate increments.

: wall? ( -- f ) head 2@ 1 arena-height within swap
                         1 arena-width  within and 0= ;
  \ Has the snake crash the wall?

: crossing? ( -- f )
  length @ 1 ?do  i segment cross? if unloop true exit then
             loop false ;
  \ Is the snake crossing itself?

: apple? ( -- f ) head apple clash? ;
  \ Has the snake found the apple?

: dead? ( -- f ) wall? crossing? or ;
  \ Is the snake dead?

: .wall ( -- )
  0 0 at-xy
  arena-width  0 ?do ." +" loop
  arena-height 0 ?do arena-width i at-xy ." +" cr ." +" loop
  arena-width  0 ?do ." +" loop cr ;
  \ Display the wall.

: unhome ( -- ) cols 1- rows 1- at-xy ;
  \ Set the cursor at the bottom right position of the screen.

: .head ( -- ) head 2@ at-xy ." O" ;
  \ Display the head of the snake.

: .neck ( -- ) neck 2@ at-xy ." o" ;
  \ Display the "neck" of the snake, ie. the first segment
  \ after the head.

: -tail ( -- ) tail 2@ at-xy space ;
  \ Delete the tail of the snake.

: .snake ( -- ) .head .neck -tail unhome ;
  \ Display the snake.

: init-arena ( -- ) page .wall .snake .apple .score ;
  \ Init the arena.

: new-snake ( -- )
  head> off 3 length !
  arena-width 2/ arena-height 2/ snake 2!
  up direction 2!
  left crawl left crawl left crawl left crawl ;
  \ Create a new snake with default values (length, position
  \ and direction).

: init-delay ( -- ) initial-delay delay ! ;
  \ Init the delay.

: init ( -- ) init-delay new-snake new-apple init-arena ;
  \ Init the game.

k-down  value down-key
k-left  value left-key
k-right value right-key
k-up    value up-key
  \ Keyboard events used as direction keys.

: key>direction ( u -- n1 n2 )
  case
    down-key  of down  endof
    left-key  of left  endof
    right-key of right endof
    up-key    of up    endof
    direction 2@ rot
   endcase ;
   \ If keyboard event _u_ is a valid direction key, return its
   \ corresponding direction _n1 n2_; otherwise return the
   \ current direction.

: (rudder) ( -- n1 n2 )
  ekey ekey>fkey if   key>direction 2dup direction 2!
                 else direction 2@ then ;
  \ Use the latest keyboard event to update the current
  \ direction and return it as _n1 n2_.  If the keyboard event
  \ is not valid, return the current direction.

: rudder ( -- n1 n2 ) ekey? if   (rudder)
                            else direction 2@ then ;
  \ If a key event is available, use to calculate a new
  \ direction; otherwise return the current one.

: lazy ( -- ) delay @ ms ;
  \ Wait the current delay.

: eat ( -- ) grow new-apple .apple ;
  \ Eat the apple.

: ?eat ( -- ) apple? if eat then ;
  \ If the apple is found, eat it.

: center-y ( -- row ) rows 2/ ;
  \ Calculate Y coordinate _row_ of the center of the
  \ screen.

: center-x ( len -- col ) cols swap - 2/ ;
  \ Calculate X coordinate _col_ of the center of the
  \ screen, for a string lenght _len_.

: center-xy ( len -- col row ) center-x center-y ;
  \ Calculate coordinates _col row_ of the center of the
  \ screen, for a string lenght _len_.

: at-center-xy ( len -- ) center-xy at-xy ;
  \ Set cursor at the center of the screen, for a string lenght
  \ _len_.

: type-center ( ca len -- ) dup at-center-xy type ;
  \ Display string _ca len_ at the center of the screen.

: +type-center ( ca len n -- )
  >r dup center-xy r> + at-xy type ;
  \ Display string _ca len_ at the center of the screen,
  \ but adding row offset _n_.

: game-over ( -- ) s" **** GAME OVER **** " type-center unhome
                   2000 ms key drop ;

: (game) ( -- ) .snake lazy rudder crawl ?eat ;
  \ Game cycle.

: game ( -- ) begin (game) dead? until game-over ;
  \ Game loop, until the snake is dead.

: version$ ( -- ca len ) s" Version " version s+ ;

: title$ ( -- ca len ) s" ooO SERPENTINO Ooo" ;

: author$ ( -- ca len ) s" programandala.net" ;

: splash-screen ( -- ) page title$ -2 +type-center
                            version$   type-center
                            author$ 2 +type-center unhome
                       2000 ms ;
  \ Display the splash screen.

: run ( -- ) begin splash-screen init game again ;
  \ Main, endless loop.

run

\ =============================================================
\ Change log

\ 2017-11-22: Fork from Robert Pfeiffer's forthsnake
\ (https://github.com/robertpfeiffer/forthsnake). Change source
\ style.  Rename words. Factor. Use constants and variables.
\ Use full screen. Draw the head apart. Document. Simplify
\ handling of directions. Remove flickering. Accelerate the
\ drawing of the snake.
