#! /usr/bin/env gforth

\ Serpentino

: version s" 0.20.0+201711231922" ;
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
\ Requirements

require galope/minus-keys.fs               \ `-keys`
require galope/question-one-minus-store.fs \ `?1-!`
require galope/random-within.fs            \ `random-within`
require galope/unhome.fs                   \ `unhome`

\ ==============================================================

variable score
variable record record off

variable delay
  \ Crawl delay in ms.

192 constant initial-delay
  \ Initial crawl delay in ms.

4 constant acceleration
  \ Delay decrement.

3 constant initial-length
  \ Initial length of the snake.

512 constant max-max-length
  \ Limit of the calculated maximum length of the snake, no
  \ matter the size of the screen. This limit makes sure the
  \ buffer is big enough, no matter if the size of the screen
  \ is increased.

: (max-length) ( -- n ) rows cols * 5 / max-max-length min ;
  \ Return maximum length _n_ of the snake, calculated after
  \ the current size of the screen.

(max-length) value max-length
  \ Maximum length of the snake.

1 constant arena-x
1 constant arena-y
  \ Coordinates of the top-left of the arena, not including
  \ the wall.

cols 1-  constant arena-width
rows 2 - constant arena-length
  \ Size of the arena, not including the wall.

2 cells constant /segment
  \ Size of each snake's segment.

create snake  max-max-length /segment * allot
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
  \ its second segment.

: tail ( -- a ) length @ segment ;
  \ Return address _a_ of the snake's tail segment.

: clash? ( a1 a2 -- f ) 2@ rot 2@ d= ;
  \ Are the coordinates contained in _a1_ equal to the
  \ coordinates contained in _a2_?

: cross? ( a -- f ) head clash? ;
  \ Does the head cross segment _a_?

: random-coordinates ( -- col row )
  1 arena-width  random-within
  1 arena-length random-within ;

: new-apple ( -- ) random-coordinates apple 2! ;
  \ Locate a new apple.

rows 1- constant status-y ( -- row )
  \ Row where the score is displayed.

: score$ ( -- ca len ) s" Score: " ;
  \ Return the score label _ca len_.

: score-xy ( -- col row ) 1 status-y ;
  \ Return the coordinates _col row_ of the score label.

: .score$ ( -- ) score-xy at-xy score$ type ;
  \ Display the score label.

: .(score) ( n -- ) s>d <# # # # # #> type ;

: .score ( -- )
  [ score-xy >r score$ nip + r> ] 2literal at-xy
  score @ .(score) ;
  \ Display the score.

: record$ ( -- ca len ) s" Record: " ;
  \ Return the record label _ca len_.

: record-xy ( -- col row )
  [ cols 1- 4 - record$ nip - ] literal status-y ;
  \ Return the coordinates _col row_ of the record label.

: .record$ ( -- ) record-xy at-xy record$ type ;
  \ Display the record label.

: .record ( -- )
  [ record-xy >r record$ nip + r> ] 2literal at-xy
  record @ .(score) ;
  \ Display the record.

: grow ( -- ) length @ 1+ max-length min length ! ;
  \ Grow the snake.

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

: wall? ( -- f ) head 2@ arena-y arena-length within swap
                         arena-x arena-width  within and 0= ;
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
  arena-length 0 ?do arena-width i at-xy ." +" cr ." +" loop
  arena-width  0 ?do ." +" loop cr ;
  \ Display the wall.

: .head ( -- ) head 2@ at-xy ." O" ;
  \ Display the head of the snake.

: .neck ( -- ) neck 2@ at-xy ." o" ;
  \ Display the "neck" of the snake, ie. its second segment.

: -tail ( -- ) tail 2@ at-xy space ;
  \ Delete the tail of the snake.

: .snake ( -- ) .head .neck -tail unhome ;
  \ Display the snake.

: .status ( -- ) .score$ .score .record$ .record ;
  \ Display the status bar.

: init-arena ( -- ) page .wall .status .apple .snake ;
  \ Init the arena.

: init-max-length ( -- ) (max-length) to max-length ;
  \ Init the maximum length of the snake, depending on the
  \ current size of the screen.

: new-snake ( -- )
  init-max-length
  head> off initial-length length !
  arena-width 2/ arena-length 2/ snake 2!
  up direction 2!  up crawl up crawl up crawl up crawl ;
  \ Create a new snake with default values (length, position
  \ and direction).

: init-delay ( -- ) initial-delay delay ! ;
  \ Init the delay.

: init ( -- )
  score off init-delay new-snake new-apple init-arena ;
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

: dodge ( -- ) score ?1-! .score ;
  \ Decrement the score because of the dodge.

: dodge? ( n1 n2 -- ) direction 2@ rot + -rot + or 0<> ;
  \ Do direction increments _n1 n2_ are a dodge, ie. do they
  \ change the current direction to the left or to the right?

: ?dodge ( n1 n2 -- ) dodge? if dodge then ;
  \ Manage a possible dodge caused by direction increments _n1
  \ n2_.

: valid-key? ( -- false | u true  ) ekey ekey>fkey ;

: (rudder) ( -- n1 n2 )
  valid-key? if   key>direction 2dup ?dodge
                                2dup direction 2!
             else direction 2@ then ;
  \ Use the latest keyboard event to update the current
  \ direction and return it as _n1 n2_.  If the keyboard event
  \ is not valid, return the current direction.

: rudder ( -- n1 n2 ) ekey? if   (rudder)
                            else direction 2@ then ;
  \ If a key event is available, use it to calculate a new
  \ direction _n1 n2_; otherwise return the current one.

: lazy ( -- ) delay @ ms ;
  \ Wait the current delay.

: faster ( -- ) delay @ acceleration - 0 max delay ! ;
  \ Decrement the delay.

: eaten ( -- ) 10 score +! .score ;
  \ Increase and display the score.

: eat ( -- ) grow faster eaten new-apple .apple ;
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

: update-record ( -- ) score @ record @ max record ! .record ;

: game-over ( -- )
  update-record
  s" **** GAME OVER **** " type-center unhome
  500 ms -keys key drop ;

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
\ drawing of the snake. Add an actual scoring.
\
\ 2017-11-23: Add record. Improve the scoring calculation.
